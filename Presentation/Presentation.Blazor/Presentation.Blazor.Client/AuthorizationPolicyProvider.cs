using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Presentation.Blazor.Client.Services.Api;

namespace Presentation.Blazor.Client;

internal class AuthorizationPolicyProvider(
    IOptions<AuthorizationOptions> options,
    IPermissionClient permissionClient)
    : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }

    private string[]? _permissions;
    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Fetch permissions from the API
        _permissions ??= await permissionClient.GetPermissionsAsync();

        if (!_permissions.Contains(policyName))
            return await _fallbackPolicyProvider.GetPolicyAsync(policyName);
        
        var policy = new AuthorizationPolicyBuilder()
            .RequireClaim("granted-permission", policyName)
            .Build();

        return policy;
    }
}
