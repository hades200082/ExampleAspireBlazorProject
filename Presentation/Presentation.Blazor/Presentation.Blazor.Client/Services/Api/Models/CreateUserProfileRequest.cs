using System.Security.Claims;
using Shared.Extensions;

namespace Presentation.Blazor.Client.Services.Api.Models;

public record CreateUserProfileRequest
{
    public CreateUserProfileRequest(ClaimsPrincipal user)
    {
        Email = user.GetEmail() ?? throw new InvalidOperationException("Cannot set user email as null");
        Name = user.GetName() ?? throw new InvalidOperationException("Cannot set user name as null");;
    }
    
    public string Email { get; }
    public string Name { get; }
}