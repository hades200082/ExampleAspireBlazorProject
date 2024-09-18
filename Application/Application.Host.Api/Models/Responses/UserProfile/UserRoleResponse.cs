using Application.CQRS.Queries.UserProfile;

namespace Application.Host.Api.Models.Responses.UserProfile;

public record UserRoleResponse
{
    public UserRoleResponse(UserRoleDto dto)
    {
        Id = dto.Id;
        Name = dto.Name;
    }

    public Ulid Id { get; set; }
    public string Name { get; set; }
}