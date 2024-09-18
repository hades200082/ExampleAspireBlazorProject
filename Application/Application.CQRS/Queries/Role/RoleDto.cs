namespace Application.CQRS.Queries.Role;

public sealed record RoleDto
{
    public RoleDto(Domain.Entities.Role entity)
    {
        Id = entity.Id;
        Name = entity.Name;
        Description = entity.Description;
        Permissions = entity.Permissions.ToArray();
        Locked = entity.Locked;
    }

    public Ulid Id { get; init; }
    public string Name { get; init; }
    public string? Description { get; init; }
    public string[] Permissions { get; init; }
    public bool Locked { get; init; }
}