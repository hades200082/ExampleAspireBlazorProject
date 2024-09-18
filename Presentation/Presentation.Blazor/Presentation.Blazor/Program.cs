using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MudBlazor.Services;
using Presentation.Blazor;
using Presentation.Blazor.Client;
using Presentation.Blazor.Client.Services.Api;
using Presentation.Blazor.Components;
using Presentation.Blazor.Extensions;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Add default Aspire services to the container.
builder.AddServiceDefaults();
builder.Services.AddMudServices();

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHsts(options =>
    {
        options.IncludeSubDomains = false;
        options.MaxAge = TimeSpan.FromDays(365);
        options.Preload = true;
    });
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddDefaultPolicy(cfg =>
        {
            cfg.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                .WithMethods("DELETE", "GET", "POST", "PUT", "PATCH", "OPTIONS")
                .AllowAnyHeader();
        });
    }
    else
    {
        var domains = builder.Configuration.GetSection("CorsDomains").Get<string[]>() ?? [];
        options.AddDefaultPolicy(cfg =>
        {
            cfg.WithOrigins(domains)
                .WithMethods("DELETE", "GET", "POST", "PUT", "PATCH", "OPTIONS")
                .AllowAnyHeader();
        });
    }
});

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, opt =>
    {
        opt.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        opt.Authority = builder.Configuration["Authentication:Authority"];
        opt.ClientId = builder.Configuration["Authentication:ClientId"];
        opt.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Authentication:RequireHttpsMetadata");
        opt.UsePkce = true;
        opt.ResponseType = OpenIdConnectResponseType.Code;
        opt.SaveTokens = true;

        opt.MapInboundClaims = false;
        opt.TokenValidationParameters.NameClaimType = ClaimTypes.Name;
        opt.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;

        opt.TokenValidationParameters.ValidateIssuer = true;
        opt.TokenValidationParameters.ValidIssuer = builder.Configuration["Authentication:Authority"];
        opt.TokenValidationParameters.ValidAudiences = [builder.Configuration["Authentication:Audience"]];
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

builder.Services.ConfigureCookieOidcRefresh(CookieAuthenticationDefaults.AuthenticationScheme,
    OpenIdConnectDefaults.AuthenticationScheme);

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddScoped<AuthenticationStateProvider, PersistingAuthenticationStateProvider>();

builder.Services.AddHttpForwarderWithServiceDiscovery();
builder.Services.AddHttpContextAccessor();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]?.TrimEnd('/') + "/";
ArgumentException.ThrowIfNullOrEmpty(apiBaseUrl);
builder.Services
    .AddHttpClient("api", client => client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute))
    .AddStandardResilienceHandler();
builder.Services.AddServerApiClient<IUserProfileClient, UserProfileClient>(apiBaseUrl);
builder.Services.AddServerApiClient<IPermissionClient, PermissionClient>(apiBaseUrl);
builder.Services.AddServerApiClient<IRoleClient, RoleClient>(apiBaseUrl);

var app = builder.Build();
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapForwarder("/api/{**catch-all}", apiBaseUrl, builder =>
{
    builder.AddXForwardedFor();
    builder.AddRequestTransform(async ctx =>
    {
        var originalPath = ctx.HttpContext.Request.Path.Value;
        
        if (originalPath!.StartsWith("/api"))
        {
            var newPath = originalPath["/api/".Length..]; // Removes "/api"
            ctx.ProxyRequest.RequestUri = new Uri($"{apiBaseUrl}{newPath}");
        }
        
        var accessToken = await ctx.HttpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(accessToken))
            ctx.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    });
});

app.UseStaticFiles();
app.UseAntiforgery();
app.UseCors();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Presentation.Blazor.Client._Imports).Assembly);

app.MapGroup("/authentication").MapLoginAndLogout();

app.MapDefaultEndpoints();

app.Run();