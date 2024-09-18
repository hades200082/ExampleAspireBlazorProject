using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using HeimGuard;
using Infrastructure.Authentication.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Shared.Enums;

namespace Infrastructure.Authentication;

public static class Extensions
{
    public static IAuthenticationOptions? AddAuthenticationOptions(this IHostApplicationBuilder builder)
    {
        var options = new AuthenticationOptions(builder.Configuration.GetSection("Authentication"));
                
        // If no authentication included log a warning and move on
        if (options.AuthenticationProvider is null) 
            return null;
        
        // Otherwise we assume we want authentication
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Authority);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Audience);
        
        builder.Services.AddSingleton<IAuthenticationOptions>(options);
        
        switch (options.AuthenticationProvider)
        {
            case AuthenticationProviders.Keycloak:
                builder.Services.AddHttpClient<IUserManager, KeycloakUserManager>(KeycloakUserManager.ConfigureHttpClient);
                break;
            case AuthenticationProviders.Auth0:
                throw new NotImplementedException();
                break;
            case AuthenticationProviders.AzureAdB2C:
                throw new NotImplementedException();
                break;
            case AuthenticationProviders.EntraExternalID:
                throw new NotImplementedException();
                break;
            case AuthenticationProviders.Logto:
                throw new NotImplementedException();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        return options;        
    }
    
    public static IHostApplicationBuilder AddAuthentication(this IHostApplicationBuilder builder)
    {
        var options = builder.AddAuthenticationOptions();
        if (options is null) return builder;
        
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(cfg =>
            {
                cfg.Audience = options.Audience;
                cfg.Authority = options.Authority;

                if (options.MetadataAddress is not null)
                    cfg.MetadataAddress = options.MetadataAddress;
                
                cfg.RequireHttpsMetadata = options.RequireHttpsMetadata;
                
                cfg.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = ClaimTypes.NameIdentifier,
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidIssuer = options.Authority,
                    ClockSkew = TimeSpan.FromSeconds(5),
                    ValidAudience = options.Audience,
                };

                cfg.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async ctx =>
                    {
                        var roleProvider = ctx.HttpContext.RequestServices.GetRequiredService<IRoleProvider>();
                        ArgumentNullException.ThrowIfNull(ctx.Principal?.Identity?.Name);
                        var roles = await roleProvider.GetUserRoles(ctx.Principal.Identity.Name);

                        if (roles.Length != 0)
                        {
                            var claims = roles
                                .Select(role => new Claim(ClaimTypes.Role, role));
                            var appIdentity = new ClaimsIdentity(claims);
                            ctx.Principal.AddIdentity(appIdentity);
                        }
                    }
                };
                
                Log.Debug("Authentication Options: {options}", JsonSerializer.Serialize(options));
            });

        builder.Services.AddHeimGuard<UserPolicyHandler>()
            .AutomaticallyCheckPermissions()
            .MapAuthorizationPolicies();
        
        return builder;
    }

    
}