using System.Text.Json.Serialization;
using Application.CQRS.Commands.UserProfile;
using Application.CQRS.Queries.UserProfile;
using Application.Host.Api.Attributes;

namespace Application.Host.Api.Models.Responses.UserProfile;

internal sealed record UserProfileResponse
{
    public UserProfileResponse(UserProfileCreatedDto dto)
    {
        Id = dto.Id;
        Email = dto.Email;
        Name = dto.Name;
    }

    public UserProfileResponse(UserProfileSummaryDto dto)
    {
        Id = dto.Id;
        Email = dto.Email;
        Name = dto.Name;
    }

    public UserProfileResponse(UserProfileDto dto)
    {
        Id = dto.Id;
        Email = dto.Email;
        Name = dto.Name;
        Roles = dto.Roles.Select(x => new UserRoleResponse(x)).ToArray();
        Permissions = dto.Permissions;
    }

    public string Id { get; init; }
    
    [Mask(MaskType.Email)]
    public string Email { get; init; }
    
    [Mask(MaskType.Name)]
    public string Name { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Redact]
    public UserRoleResponse[]? Roles { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Redact]
    public string[]? Permissions { get; set; }
    
    #region Custom properties
    
    /*
     * Add your custom properties here
     */
    
    #endregion
}