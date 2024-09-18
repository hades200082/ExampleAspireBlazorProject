using System.ComponentModel.DataAnnotations;

namespace Application.Host.Api.Models.Requests.UserProfile;

/// <summary>
/// Used when creating a new user profile.
/// </summary>
public sealed class CreateUserProfileRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
    
    [Required]
    [MinLength(3)]
    public required string Name { get; set; }
}