using Shared.Enums;

namespace Infrastructure.Authentication.Abstractions;

public interface IAuthenticationOptions
{
    AuthenticationProviders? AuthenticationProvider { get; }
    string? Audience { get; }
    string? Authority { get; }
    string? SwaggerClientId { get; }
    string? ManagementClientId { get; }
    string? ManagementClientSecret { get; }
    string? MetadataAddress { get; }
    bool RequireHttpsMetadata { get; }
    
}