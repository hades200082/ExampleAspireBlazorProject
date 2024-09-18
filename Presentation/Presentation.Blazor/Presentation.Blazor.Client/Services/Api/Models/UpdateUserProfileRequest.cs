using System.Security.Claims;
using Shared.Extensions;

namespace Presentation.Blazor.Client.Services.Api.Models;

public record UpdateUserProfileRequest(string Email, string Name)
{
    public UpdateUserProfileRequest(ClaimsPrincipal user) 
        : this(user.GetEmail() 
               ?? throw new InvalidOperationException("Cannot set user email as null"), 
            user.GetName() 
            ?? throw new InvalidOperationException("Cannot set user name as null"))
    {
    }

    public UpdateUserProfileRequest(UserProfile user) : this(user.Email, user.Name)
    {
    }
    
    public string Email { get; } = Email;
    public string Name { get; } = Name;
}