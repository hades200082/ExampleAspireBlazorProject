using System.Security.Claims;

namespace Shared.Extensions;

public static class ClaimsIdentityExtensions
{
    public static string? GetUserId(this ClaimsIdentity user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               user.FindFirst("sub")?.Value;
    }
    public static string? GetEmail(this ClaimsIdentity user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value;
    }
    public static string? GetName(this ClaimsIdentity user)
    {
        var val = user.FindFirst(ClaimTypes.Name)?.Value 
                  ?? user.FindFirst("name")?.Value;

        if (!string.IsNullOrEmpty(val)) return val;
        
        var fname = user.FindFirst(ClaimTypes.GivenName)?.Value
                    ?? user.FindFirst("given_name")?.Value
                    ?? user.FindFirst("first_name")?.Value;

        var lname = user.FindFirst(ClaimTypes.Surname)?.Value
                    ?? user.FindFirst("family_name")?.Value
                    ?? user.FindFirst("last_name")?.Value
                    ?? user.FindFirst("surname")?.Value;
        val = $"{fname} {lname}";

        return val;
    }
    public static string? GetFirstName(this ClaimsIdentity user)
    {
        return user.FindFirst(ClaimTypes.GivenName)?.Value
               ?? user.FindFirst("given_name")?.Value
               ?? user.FindFirst("first_name")?.Value
               ?? GetName(user)?.Split(' ').FirstOrDefault();
    }
    public static string? GetLastName(this ClaimsIdentity user)
    {
        return user.FindFirst(ClaimTypes.Surname)?.Value
               ?? user.FindFirst("family_name")?.Value
               ?? user.FindFirst("last_name")?.Value
               ?? user.FindFirst("surname")?.Value
               ?? GetName(user)?.Split(' ').LastOrDefault();
    }
}