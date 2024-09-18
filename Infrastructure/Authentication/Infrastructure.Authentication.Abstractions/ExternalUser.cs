namespace Infrastructure.Authentication.Abstractions;

public sealed class ExternalUser
{
    public string Id { get; set; }

    public string Name { get; set; }
    public string GivenName { get; set; }
    public string FamilyName { get; set; }
    public string PreferredUsername { get; set; }

    public string? PictureUrl { get; set; }

    public string Email { get; set; }
    public bool EmailVerified { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberVerified { get; set; }

    public Dictionary<string, List<string>> CustomData { get; set; }
    public bool Enabled { get; set; }
}