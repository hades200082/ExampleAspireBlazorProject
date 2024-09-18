using System.Net;
using System.Net.Http.Json;
using Presentation.Blazor.Client.Services.Api.Models;
using Shared.Abstractions;

namespace Presentation.Blazor.Client.Services.Api;

internal interface IRoleClient :
    IApiGetClient<Ulid, Role>,
    IApiCreateClient<CreateRoleRequest, Role>,
    IApiUpdateClient<Ulid, Role>,
    IApiDeleteClient<Ulid>
{
    Task UpdatePermissions(Ulid id, string[] permissions, CancellationToken cancellationToken = default);
}

public class RoleClient(HttpClient client) : IRoleClient
{
    private const string BasePath= "v1/roles";
    public async Task<Role?> GetAsync(Ulid id, CancellationToken cancellationToken = default)
    {
        var response = await client.GetAsync($"{BasePath}/{id}", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Role>(
                cancellationToken: cancellationToken);
        }

        if(response.StatusCode == HttpStatusCode.NotFound)
            return null;
        
        throw new ApiResponseException(response.StatusCode, (await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken))!);
    }

    public async Task<IPagedDataSet<Role>> GetAsync(int page, int perPage = 10, string? filter = null, string? order = null,
        CancellationToken cancellationToken = default)
    {
        var query = $"page={page}&recordsPerPage={perPage}";
        if(!string.IsNullOrEmpty(filter)) query += $"&filter={filter}";
        if(!string.IsNullOrEmpty(order)) query += $"&order={order}";
        
        var response = await client.GetAsync($"{BasePath}?{query}", cancellationToken);
        if(response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<PagedDataSet<Role>>(cancellationToken))!;
        
        throw new ApiResponseException(response.StatusCode, (await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken))!);
    }

    public async Task<Role> CreateAsync(CreateRoleRequest model, CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsJsonAsync(BasePath, model, cancellationToken);
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<Role>(cancellationToken: cancellationToken))!;
        
        throw new ApiResponseException(response.StatusCode, (await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken))!);
    }

    public async Task UpdateAsync(Ulid id, Role model, CancellationToken cancellationToken = default)
    {
        var response = await client.PutAsJsonAsync($"{BasePath}/{id}", model, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new ApiResponseException(response.StatusCode, (await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken))!);
    }

    public async Task DeleteAsync(Ulid id, CancellationToken cancellationToken = default)
    {
        var response = await client.DeleteAsync($"{BasePath}/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new ApiResponseException(response.StatusCode, (await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken))!);
    }

    public async Task UpdatePermissions(Ulid id, string[] permissions, CancellationToken cancellationToken = default)
    {
        var response = await client.PutAsJsonAsync($"{BasePath}/{id}/permissions", permissions, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new ApiResponseException(response.StatusCode, (await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken))!);
    }
}