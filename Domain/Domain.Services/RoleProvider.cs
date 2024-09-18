using Domain.Entities;
using Infrastructure.Authentication.Abstractions;
using Infrastructure.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;

namespace Domain.Services;

public sealed class RoleProvider(AppDbContext db, DatabaseOptions dbOptions) : IRoleProvider
{
    public async Task<string[]> GetUserRoles(string externalId)
    {
        IQueryable<UserProfile> query = db.UserProfiles
            .Include(x => x.Roles);

        if (dbOptions.DatabaseProvider == DatabaseProviders.CosmosDB)
            query = query.WithPartitionKey(nameof(UserProfile));

        var userProfile = await query.SingleOrDefaultAsync(x => x.Id == externalId);
        return userProfile is null
            ? []
            : userProfile
                .Roles
                .Select(x => x.Name)
                .ToArray();
    }
}