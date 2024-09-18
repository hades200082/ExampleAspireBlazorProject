using System.ComponentModel.DataAnnotations;

namespace Application.Host.Api.Models.Requests.UserProfile;

/// <summary>
/// Used when updating a whole user profile
/// </summary>
public sealed class UpdateUserProfileRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
    
    [Required]
    [MinLength(3)]
    public required string Name { get; set; }
}