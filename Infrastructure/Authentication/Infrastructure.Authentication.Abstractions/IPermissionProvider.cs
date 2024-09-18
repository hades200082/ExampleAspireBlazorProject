using Domain.Entities;

namespace Infrastructure.Authentication.Abstractions;

public interface IPermissionProvider
{
    Task<string[]> GetPermissionsForRoles(IEnumerable<string> roles);
    string[] GetPermissionsByEntity(Type entityType);
    Task<string[]> GetUserPermissions(string userId);
    Task<string[]> GetUserPermissions(UserProfile userProfile);
    Task<string[]> GetAllPermissions(CancellationToken cancellationToken = default);
}