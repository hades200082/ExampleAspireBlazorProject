using Infrastructure.Authentication.Abstractions;
using Microsoft.Extensions.Configuration;
using Shared.Enums;

namespace Infrastructure.Authentication;

public class AuthenticationOptions(IConfigurationSection config) : IAuthenticationOptions
{

    public AuthenticationProviders? AuthenticationProvider =>
        config.GetValue<AuthenticationProviders>("AuthenticationProvider");

    /// <summary>
    /// The audience against which we will validate JWTs. If the `aud` claim in the JWT
    /// does not exactly match this value, the JWT will be rejected and the request not authorised.
    /// </summary>
    public string? Audience => config.GetValue<string>("Audience");

    private readonly string? _authority = config.GetValue<string>("Authority");
    public string? Authority
    {
        get
        {
            if (AuthenticationProvider == AuthenticationProviders.Keycloak && _authority is null)
            {
                return $"{AuthServerUrl}/realms/{Realm}";
            }

            return _authority;
        }
    }
    
    /// <summary>
    /// Not required in production
    /// </summary>
    public string? SwaggerClientId => config.GetValue<string>("SwaggerClientId");
    
    public string? ManagementClientId => config.GetValue<string>("ManagementClientId");
    public string? ManagementClientSecret => config.GetValue<string>("ManagementClientSecret");
    

    private readonly string? _metadataAddress = config.GetValue<string>("MetadataAddress");

    public string? MetadataAddress
    {
        get
        {
            if (AuthenticationProvider == AuthenticationProviders.Keycloak && _metadataAddress is null)
            {
                return $"{AuthServerUrl}/realms/{Realm}/.well-known/openid-configuration";
            }

            return _metadataAddress;
        }
    }

    public bool RequireHttpsMetadata => config.GetValue("RequireHttpsMetadata", true);
    
    // For Keycloak
    private string? AuthServerUrl => config.GetValue<string>("AuthServerUrl");
    private string? Realm => config.GetValue<string>("Realm");
}