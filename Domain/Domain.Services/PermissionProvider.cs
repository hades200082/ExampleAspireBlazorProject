using System.Text.Json;
using Domain.Abstractions;
using Domain.Entities;
using Infrastructure.Authentication.Abstractions;
using Infrastructure.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Shared.Enums;

namespace Domain.Services;

public sealed class PermissionProvider(
    AppDbContext db,
    DatabaseOptions dbOptions,
    IDistributedCache cache
) : IPermissionProvider
{
    public async Task<string[]> GetPermissionsForRoles(IEnumerable<string> roles)
    {
        roles = roles.Select(rn => rn.ToUpper()).ToList();

        if (roles.Contains(Role.GlobalAdministrator.ToUpper())) return await GetAllPermissions();

        var query = db.Roles.Where(r => roles.Contains(r.Name));

        if (dbOptions.DatabaseProvider == DatabaseProviders.CosmosDB)
            query = query.WithPartitionKey(nameof(Role));

        return await query.SelectMany(p => p.Permissions)
            .Distinct()
            .ToArrayAsync();
    }

    private const string AllPermissionsCacheKey = "AllPermissions";

    public async Task<string[]> GetAllPermissions(CancellationToken cancellationToken = default)
    {
        var cachedVal = await cache.GetStringAsync(AllPermissionsCacheKey, cancellationToken);

        if (string.IsNullOrEmpty(cachedVal))
        {
            var data = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.DefinedTypes)
                .Where(t => !t.IsInterface && !t.IsAbstract && t.ImplementedInterfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>)))
                .SelectMany(t => GetPermissionsByEntity(t.AsType()))
                .ToList();

            // Add specific Non-entity-based permissions here
            
            await cache.SetStringAsync(AllPermissionsCacheKey, JsonSerializer.Serialize(data.ToArray()), new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1),
                SlidingExpiration = TimeSpan.FromMinutes(5)
            }, cancellationToken);
            return data.Distinct().ToArray();
        }

        var cachedData = JsonSerializer.Deserialize<string[]>(cachedVal);
        if (cachedData is not null) return cachedData;
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.DefinedTypes
                .Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(IEntity<>))))
            .SelectMany(t => GetPermissionsByEntity(t.AsType()))
            .ToArray();
    }

    public string[] GetPermissionsByEntity(Type entityType)
    {
        var name = entityType.Name;
        return entityType switch
        {
            not null when entityType == typeof(UserProfile) =>
            [
                $"{nameof(UserProfile)}:Create", $"{nameof(UserProfile)}:Read", $"{nameof(UserProfile)}:Update",
                $"{nameof(UserProfile)}:Delete", $"{nameof(UserProfile)}:ManageRoles"
            ],
            not null when entityType == typeof(Role) =>
            [
                $"{nameof(Role)}:Create", $"{nameof(Role)}:Read", $"{nameof(Role)}:Update",
                $"{nameof(Role)}:Delete", $"{nameof(Role)}:ManagePermissions"
            ],
            _ =>
            [
                $"{name}:Create", $"{name}:Read", $"{name}:Update",
                $"{name}:Delete",
            ]
        };
    }

    public async Task<string[]> GetUserPermissions(string userId)
    {
        var roles = (await db.UserProfiles
                .Include(x => x.Roles)
                .Where(x => x.Id == userId)
                .FirstOrDefaultAsync())
            ?.Roles.Select(x => x.Name) ?? [];

        // TODO : Cache this in IDistributedCache and provide mechanism for clearing the cache when permissions/roles change
        return await GetPermissionsForRoles(roles);
    }

    public async Task<string[]> GetUserPermissions(UserProfile userProfile)
    {
        if (userProfile.Roles.Any())
        {
            return await GetPermissionsForRoles(userProfile.Roles.Select(x => x.Name));
        }
        
        return await GetUserPermissions(userProfile.Id);
    }
}