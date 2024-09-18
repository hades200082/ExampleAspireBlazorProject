namespace Application.CQRS.Commands.UserProfile;

public sealed record UserProfileCreatedDto
{
    public UserProfileCreatedDto(Domain.Entities.UserProfile entity)
    {
        Id = entity.Id;
        Email = entity.Email;
        Name = entity.Name;
    }

    public string Id { get; init; }
    public string Email { get; init; }
    public string Name { get; init; }
}