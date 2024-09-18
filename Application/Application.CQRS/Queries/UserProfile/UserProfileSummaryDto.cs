namespace Application.CQRS.Queries.UserProfile;

public sealed record UserProfileSummaryDto
{
    public UserProfileSummaryDto(Domain.Entities.UserProfile entity)
    {
        Id = entity.Id;
        Email = entity.Email;
        Name = entity.Name;
    }

    public string Id { get; init; }
    public string Email { get; init; }
    public string Name { get; init; }
}