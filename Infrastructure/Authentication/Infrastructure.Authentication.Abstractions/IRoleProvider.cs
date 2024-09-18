namespace Infrastructure.Authentication.Abstractions;

public interface IRoleProvider
{
    Task<string[]> GetUserRoles(string externalId);
}