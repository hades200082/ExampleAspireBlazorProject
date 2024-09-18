using System.Reflection;
using Asp.Versioning.ApiExplorer;
using Infrastructure.Authentication.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Application.Host.Api;

internal sealed class ConfigureSwaggerOptions(
    IApiVersionDescriptionProvider provider,
    IAuthenticationOptions authOptions,
    IConfiguration configuration
) : IConfigureNamedOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                CreateVersionInfo(description));
        }
        
        if (!string.IsNullOrEmpty(authOptions.Authority))
        {
            options.AddSecurityDefinition("openidconnect", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OpenIdConnect,
                OpenIdConnectUrl = new Uri(authOptions.MetadataAddress!,
                    UriKind.Absolute),
                Description = "OpenID Connect",
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        // The AuthorizationUrl and TokenUrl should be discovered, so you don't need to set them explicitly.
                        Scopes = new Dictionary<string, string>
                        {
                            { "openid", "OpenID" },
                            { "profile", "Profile" }
                        }
                    }
                }
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "openidconnect"
                        }
                    },
                    new List<string> { "openid", "profile" }
                }
            });
        }

        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

        // Ensure that Swagger docs matches the format for DateOnly in Shared.Core.DateOnlyJsonConverter
        options.SchemaGeneratorOptions.CustomTypeMappings.Add(typeof(DateOnly),
            () => new OpenApiSchema {Type = "string", Format = "date"});
        
        // Map `Ulid` to `string` in Swagger
        options.SchemaGeneratorOptions.CustomTypeMappings.Add(typeof(Ulid),
            () => new OpenApiSchema { Type = "string" });
        
        options.OperationFilter<JsonPatchDocumentOperationFilter>();
    }

    public void Configure(string? name, SwaggerGenOptions options)
    {
        Configure(options);
    }

    private OpenApiInfo CreateVersionInfo(ApiVersionDescription description)
    {
        var environmentUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7285";

        var info = new OpenApiInfo
        {
            Title = "TemplateProject API",
            Version = $"v{description.ApiVersion}",
            Contact = new OpenApiContact {Name = "Lee Conlin", Url = new Uri(environmentUrl)},
            Extensions = new Dictionary<string, IOpenApiExtension>(StringComparer.Ordinal)
            {
                {
                    "x-logo", new OpenApiObject
                    {
                        {"url", new OpenApiString("/logo.png")},
                        {"altText", new OpenApiString("Logo")},
                    }
                },
            },
        };

        var filesPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Docs",
            $"v{description.ApiVersion.MajorVersion}");

        // Get all *.md files from the filesPath directory and concatenate them into a single string with a single blank line between each file
        var markdownFiles = Directory.GetFiles(filesPath, "*.md");
        var markdownContent = string.Join("\r\n\r\n", markdownFiles.Select(File.ReadAllText));

        if (description.IsDeprecated)
        {
            info.Description += "**This API version has been deprecated.**";
        }

        info.Description += "<br><details><summary>Versions</summary>\r\n";

        var latestVersion = provider
            .ApiVersionDescriptions.Max(v => v.ApiVersion.MajorVersion);
        var versionLinks =
            provider
                .ApiVersionDescriptions
                .OrderByDescending(v => v.ApiVersion)
                .Select(v =>
                {
                    var value = $"- [v{v.ApiVersion}](/v{v.ApiVersion}/api-docs/index.html)";

                    if (v.ApiVersion.MajorVersion == latestVersion)
                    {
                        value += " **(latest)**";
                    }

                    if (v.ApiVersion.MajorVersion == description.ApiVersion.MajorVersion)
                    {
                        value += " \ud83d\udc41\ufe0f";
                    }

                    return value;
                })
                .ToList();

        info.Description += $"{string.Join("\r\n", versionLinks)}</details>";

        info.Description += $"\r\n{markdownContent}";

        return info;
    }
}