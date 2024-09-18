namespace Presentation.Blazor.Client.Services.Api.Models;

public record UserProfile(string Id, string Email, string Name, UserRole[]? Roles, string[]? Permissions);
public record UserRole(Ulid Id, string Name);