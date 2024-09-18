using Domain.Services;
using Infrastructure.EntityFrameworkCore;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Enums;

namespace Application.CQRS.Commands.UserProfile;

public interface UpdateUserProfileRolesCommand : Request<IValueResult<bool>>
{
    const string NotFound = "UserProfile.UpdateRoles.NotFound";
    const string UpdateFailed = "UserProfile.UpdateRoles.Fail";
    
    public string ExternalId { get; set; }
    public Ulid[] RoleIds { get; set; }
}

public class UpdateUserProfileRolesCommandHandler(
    AppDbContext db,
    DatabaseOptions options,
    ILogger<UpdateUserProfileRolesCommandHandler> logger
) : IConsumer<UpdateUserProfileRolesCommand>
{
    public async Task Consume(ConsumeContext<UpdateUserProfileRolesCommand> context)
    {
        try
        {
            var entity = await db.UserProfiles.FindAsync(context.Message.ExternalId, context.CancellationToken);

            if (entity is null || entity.DeletedAt is not null)
            {
                logger.LogWarning("Update roles failed for user with external ID: {ID} - record not found", context.Message.ExternalId);
                await context.RespondAsync(
                    new ValueResult<bool>(UpdateUserProfileRolesCommand.NotFound, "The requested record was not found"));
                return;
            }
            await db.Entry(entity).Collection(x => x.Roles).LoadAsync(context.CancellationToken); // populate the user's roles

            var rolesQuery = db.Roles.Where(x => context.Message.RoleIds.Contains(x.Id));

            if (options.DatabaseProvider == DatabaseProviders.CosmosDB)
                rolesQuery = rolesQuery.WithPartitionKey(nameof(Domain.Entities.Role));

            entity.Roles = await rolesQuery.ToListAsync(context.CancellationToken);
            
            await db.SaveChangesAsync(context.CancellationToken);
            await context.RespondAsync(new ValueResult<bool>(true));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update roles failed for user with external ID: {ID}", context.Message.ExternalId);
            await context.RespondAsync(
                new ValueResult<bool>(UpdateUserProfileRolesCommand.UpdateFailed, "A problem was encountered while updating the users roles"));
        }
    }
}