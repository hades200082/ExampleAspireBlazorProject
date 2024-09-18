namespace Presentation.Blazor.Client.Services.Api.Models;

public record Role(Ulid Id, string Name, string? Description, string[] Permissions, bool Locked);