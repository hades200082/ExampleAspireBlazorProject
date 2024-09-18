using System.Net.Http.Json;
using Presentation.Blazor.Client.Services.Api.Models;

namespace Presentation.Blazor.Client.Services.Api;

internal interface IPermissionClient
{
    Task<string[]> GetPermissionsAsync(CancellationToken cancellationToken = default);
}

internal sealed class PermissionClient(HttpClient client) : IPermissionClient
{
    private const string BasePath= "v1/permissions";
    
    public async Task<string[]> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var response = await client.GetAsync(BasePath, cancellationToken);
        if(response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<string[]>(cancellationToken))!;
        
        throw new ApiResponseException(response.StatusCode, (await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken))!);
    }
}