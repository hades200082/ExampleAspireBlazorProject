using HeimGuard;
using Infrastructure.Authentication.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Authentication;

// ReSharper disable once ClassNeverInstantiated.Global
// Instantiated by HeimGuard from DI
public sealed class UserPolicyHandler(
    IHttpContextAccessor httpContextAccessor,
    IPermissionProvider permissionProvider
) : IUserPolicyHandler
{
    public async Task<IEnumerable<string>> GetUserPermissions()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user is null || !(user.Identity?.IsAuthenticated ?? false) || user.Identity.Name is null)
            return []; // If we don't have a user then they have no permissions

        var permissions = await permissionProvider.GetUserPermissions(user.Identity.Name);
        return permissions.Distinct();
    }
}