using System.Security.Claims;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Shared.Extensions;

namespace Presentation.Blazor.Client;

public sealed class PersistedUserInfo
{
    public required string UserId { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string[] Roles { get; init; } = [];
    public required string[] Permissions { get; init; } = [];

    public static PersistedUserInfo FromClaimsPrincipal(ClaimsPrincipal principal) =>
        new()
        {
            UserId = principal.GetUserId()!,
            Name = principal.GetName()!,
            Email = principal.GetEmail()!,
            Roles = GetClaims(principal, ClaimTypes.Role),
            Permissions = GetClaims(principal, "granted-permission")
        };

    public ClaimsPrincipal ToClaimsPrincipal() =>
        new(new ClaimsIdentity(
            GetClaims(),
            authenticationType: nameof(PersistedUserInfo),
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role));

    private Claim[] GetClaims() =>
        new[]
            {
                new Claim("sub", UserId),
                new Claim(ClaimTypes.NameIdentifier, UserId),
                new Claim(ClaimTypes.Name, Name),
                new Claim(ClaimTypes.Email, Email),
            }
            .Concat(Roles.Select(role => new Claim(ClaimTypes.Role, role)))
            .Concat(Permissions.Select(role => new Claim("granted-permission", role)))
            .ToArray();
    
    private static string[] GetClaims(ClaimsPrincipal principal, string claimType) =>
        principal.FindAll(claimType).Select(x => x.Value).ToArray();
    private static string GetRequiredClaim(ClaimsPrincipal principal, string claimType) =>
        principal.FindFirst(claimType)?.Value ?? throw new InvalidOperationException($"Could not find required '{claimType}' claim.");
}