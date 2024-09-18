namespace Application.CQRS.Queries.UserProfile;

public sealed record UserProfileDto
{
    public UserProfileDto(Domain.Entities.UserProfile entity, string[] permissions)
    {
        Id = entity.Id;
        Email = entity.Email;
        Name = entity.Name;
        Roles = entity.Roles.Select(x => new UserRoleDto(x)).ToArray();
        Permissions = permissions;
    }

    public string Id { get; init; }
    public string Email { get; init; }
    public string Name { get; init; }

    public UserRoleDto[] Roles { get; init; }

    public string[] Permissions { get; set; }
}