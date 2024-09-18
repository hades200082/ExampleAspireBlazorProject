namespace Application.CQRS.Queries.UserProfile;

public record UserRoleDto
{
    public UserRoleDto(Domain.Entities.Role entity)
    {
        Id = entity.Id;
        Name = entity.Name;
    }

    public Ulid Id { get; init; }
    public string Name { get; init; }
}