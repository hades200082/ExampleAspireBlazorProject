using System.Security.Claims;

namespace Application.Host.Api.Extensions;

internal static class ClaimsPrincipleExtensions
{
    public static string? GetIdentity(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
    
    public static string? GetPreferredUserName(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Name);
    }
    
    public static string? GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Email);
    }
}