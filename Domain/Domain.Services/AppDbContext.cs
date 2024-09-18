using System.Reflection;
using System.Text.Json;
using Domain.Abstractions;
using Domain.AMQP.MessageContracts.Events;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Domain.Services;

public class AppDbContext : DbContext
{
    #region Constructors
    public AppDbContext() : base()
    {
    }

    public AppDbContext(DbContextOptions options) : base(options)
    {
    }
    #endregion

    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Role> Roles { get; set; }

    public DbSet<EmailTemplate> EmailTemplates { get; set; }


    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
        configurationBuilder.Properties<Ulid>()
            .HaveConversion<UlidToStringConverter>()
            .HaveMaxLength(26);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("Domain.Entities"));
        
        if (!Database.ProviderName?.Contains("Cosmos") ?? true) return;
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(e => e.GetType().IsAssignableTo(typeof(IEntity<>))))
        {
            entityType.SetPartitionKeyPropertyName(nameof(IEntity<int>.PartitionKey));
        }
    }
    
    public override int SaveChanges()
    {
        SetUlidForAddedEntities();
        UpdateTimestamps();
        PrepareNotifications(); // Stores the entities that are being changed, deleted or created
        var i = base.SaveChanges();
        PublishNotifications().GetAwaiter().GetResult(); // Publishes the events
        return i;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        SetUlidForAddedEntities();
        UpdateTimestamps();
        PrepareNotifications(); // Stores the entities that are being changed, deleted or created
        var i = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        await PublishNotifications(cancellationToken); // Publishes the events
        return i;
    }

    private void SetUlidForAddedEntities()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e is {State: EntityState.Added, Entity: IEntity<Ulid>});

        foreach (var entry in entries)
        {
            ((IEntity<Ulid>) entry.Entity).Id = Ulid.NewUlid();
        }
    }

    private readonly List<KeyValuePair<string, object>> _events = [];
    private void PrepareNotifications()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity.GetType().GetTypeInfo().ImplementedInterfaces.Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>)))
            .ToList();
        
        foreach (var entityEntry in entries)
        {
            switch (entityEntry.State)
            {
                case EntityState.Detached:
                case EntityState.Unchanged:
                    continue; // Nothing to do here

                case EntityState.Deleted:
                case EntityState.Modified:
                case EntityState.Added:
                    _events.Add( new KeyValuePair<string, object>(Enum.GetName(typeof(EntityState),entityEntry.State)!, entityEntry.Entity));
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    private async Task PublishNotifications(CancellationToken cancellationToken = default)
    {
        var logger = this.GetService<ILogger<AppDbContext>>();
        var bus = this.GetService<IBus>();
        
        foreach (var (key, entity) in _events)
        {
            var entityType = entity.GetType();
            var idProperty = entityType.GetProperty("Id");
        
            if (idProperty == null)
            {
                throw new InvalidOperationException($"Entity type {entityType.Name} does not have an Id property.");
            }

            var idValue = idProperty.GetValue(entity);
            
            switch (Enum.Parse(typeof(EntityState), key))
            {
                case EntityState.Detached:
                case EntityState.Unchanged:
                    continue; // Nothing to do here
                
                case EntityState.Deleted:
                    logger.LogInformation("Entity deleted. Type: {Type}. Id: {Id}. Object: {Entity}", entityType.Name, idValue, JsonSerializer.Serialize(entity));
                    await bus.Publish<EntityDeleted>(new {Entity = entity}, cancellationToken);
                    break;
                case EntityState.Modified:
                    logger.LogInformation("Entity updated. Type: {Type}. Id: {Id}. Object: {Entity}", entityType.Name, idValue, JsonSerializer.Serialize(entity));
                    await bus.Publish<EntityUpdated>(new {Entity = entity}, cancellationToken);
                    break;
                case EntityState.Added:
                    logger.LogInformation("Entity created. Type: {Type}. Id: {Id}. Object: {Entity}", entityType.Name, idValue, JsonSerializer.Serialize(entity));
                    await bus.Publish<EntityCreated>(new {Entity = entity}, cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity.GetType().IsAssignableTo(typeof(IAuditableEntity)) &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            ((IAuditableEntity) entityEntry.Entity).UpdatedAt = DateTime.UtcNow;

            if (entityEntry.State == EntityState.Added)
            {
                ((IAuditableEntity) entityEntry.Entity).CreatedAt = DateTime.UtcNow;
            }
            else
            {
                entityEntry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
            }
        }
    }
}