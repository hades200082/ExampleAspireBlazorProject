using Application.CQRS.Queries.Role;

namespace Application.Host.Api.Models.Responses.Role;

internal sealed record RoleResponse
{
    public RoleResponse(RoleDto dto)
    {
        Id = dto.Id;
        Name = dto.Name;
        Description = dto.Description;
        Permissions = dto.Permissions.ToArray();
        Locked = dto.Locked;
    }

    public Ulid Id { get; init; }
    public string Name { get; init; }
    public string? Description { get; init; }
    public string[] Permissions { get; init; }
    public bool Locked { get; init; }
};