using Infrastructure.Authentication.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shared.Enums;
using Testcontainers.Keycloak;

namespace Infrastructure.Authentication.IntegrationTests;

public class KeycloakUserManagerTests : IAsyncLifetime
{    
    private const string UserID = "ef8ba546-2efc-4347-b81e-4a0210b9b7a3";
    
    [Fact]
    public async Task KeycloakUserManager_GetUser_ReturnsCorrectUser()
    {
        var user = await _sut.GetUser(UserID);
        
        Assert.NotNull(user);
        Assert.IsType<ExternalUser>(user);
        Assert.Equal(UserID, user.Id);
        Assert.NotEqual(string.Empty, user.Email);
        Assert.Contains("@", user.Email);
        Assert.NotNull(user.Name);
        Assert.NotEqual(string.Empty, user.Name);
    }

    [Fact]
    public async Task KeycloakUserManager_UpdateEmail_UpdatesTheUser()
    {
        const string newEmail = "update-test@test.com";
        await _sut.UpdateEmail(UserID, newEmail);
        
        var user = await _sut.GetUser(UserID);
        
        Assert.NotNull(user);
        Assert.Equal(newEmail, user.Email);
        Assert.Equal(newEmail, user.PreferredUsername);
    }
    
    [Fact]
    public async Task KeycloakUserManager_DisableEnableUser_UpdatesTheUser()
    {
        await _sut.DisableUser(UserID);
        
        var user = await _sut.GetUser(UserID);
        Assert.NotNull(user);
        Assert.Equal(UserID, user.Id);
        Assert.False(user.Enabled);
        
        await _sut.EnableUser(UserID);
        
        user = await _sut.GetUser(UserID);
        Assert.NotNull(user);
        Assert.Equal(UserID, user.Id);
        Assert.True(user.Enabled);
    }

    private readonly KeycloakContainer _keycloak = new KeycloakBuilder()
            .WithImage("quay.io/keycloak/keycloak:24.0")
            .WithBindMount(Path.GetFullPath("../../../../../../.keycloak/import", AppDomain.CurrentDomain.BaseDirectory), 
                "/opt/keycloak/data/import")
            .WithCommand("--import-realm")
            .Build();

    private KeycloakUserManager _sut;

    public async Task InitializeAsync()
    {
        await _keycloak.StartAsync();
        
        var options = Substitute.For<IAuthenticationOptions>();
        options.Audience.Returns("https://TemplateProject.api");
        options.Authority.Returns($"{_keycloak.GetBaseAddress().TrimEnd('/')}/realms/TemplateProject");
        options.AuthenticationProvider.Returns(AuthenticationProviders.Keycloak);
        options.RequireHttpsMetadata.Returns(true);
        options.ManagementClientId.Returns("admin-cli");
        options.ManagementClientSecret.Returns("admin:admin");
        
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient().Returns(new HttpClient());
        
        var serviceProvider = Substitute.For<IServiceProvider>();
        
        serviceProvider.GetService(typeof(IAuthenticationOptions)).Returns(options);
        serviceProvider.GetService<IAuthenticationOptions>().Returns(options);
        serviceProvider.GetRequiredService(typeof(IAuthenticationOptions)).Returns(options);
        serviceProvider.GetRequiredService<IAuthenticationOptions>().Returns(options);
        
        serviceProvider.GetService(typeof(IHttpClientFactory)).Returns(factory);
        serviceProvider.GetService<IHttpClientFactory>().Returns(factory);
        serviceProvider.GetRequiredService(typeof(IHttpClientFactory)).Returns(factory);
        serviceProvider.GetRequiredService<IHttpClientFactory>().Returns(factory);

        var cache = Substitute.For<IDistributedCache>();
        cache.GetAsync(Arg.Any<string>()).Returns(Task.FromResult<byte[]?>(null));
        cache.GetAsync(Arg.Any<string>(), default).Returns(Task.FromResult<byte[]?>(null));
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<byte[]?>(null));
        cache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        var client = new HttpClient();
        KeycloakUserManager.ConfigureHttpClient(serviceProvider, client);

        _sut = new KeycloakUserManager(client, options, cache);
    }

    public async Task DisposeAsync()
    {
        await _keycloak.DisposeAsync();
    }
}