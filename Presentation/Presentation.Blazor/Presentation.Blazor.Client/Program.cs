using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MudBlazor;
using MudBlazor.Services;
using Presentation.Blazor.Client;
using Presentation.Blazor.Client.Extensions;
using Presentation.Blazor.Client.Services.Api;

[assembly:InternalsVisibleTo("Presentation.Blazor")]

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices(configuration =>
{
 configuration.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
 configuration.SnackbarConfiguration.NewestOnTop = false;
 configuration.SnackbarConfiguration.ShowCloseIcon = true;
 configuration.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
 configuration.SnackbarConfiguration.PreventDuplicates = true;
 configuration.SnackbarConfiguration.MaxDisplayedSnackbars = 5;
});
builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();

/*
 * The API client in this WEB ASSEMBLY client needs to talk to the Blazor SERVER on the /api
 * endpoint. The Blazor Server will proxy that request to the real API location, adding
 * any access tokens needed.
 */
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]?.TrimEnd('/') + "/";
ArgumentException.ThrowIfNullOrEmpty(apiBaseUrl);
builder.Services.AddHttpClient("api", client => client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute)).AddStandardResilienceHandler();

builder.Services.AddApiClient<IUserProfileClient, UserProfileClient>(apiBaseUrl);
builder.Services.AddApiClient<IPermissionClient, PermissionClient>(apiBaseUrl);
builder.Services.AddApiClient<IRoleClient, RoleClient>(apiBaseUrl);

await builder.Build().RunAsync();