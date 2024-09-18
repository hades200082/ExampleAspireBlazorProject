using System.Net;
using System.Net.Http.Json;
using Presentation.Blazor.Client.Services.Api.Models;
using Shared.Abstractions;

namespace Presentation.Blazor.Client.Services.Api;

internal interface IUserProfileClient :
    IApiGetClient<string, UserProfile>,
    IApiCreateClient<CreateUserProfileRequest, UserProfile>,
    IApiUpdateClient<string, UpdateUserProfileRequest>,
    IApiDeleteClient<string>
{
    Task UpdateRoles(string id, Ulid[] roleIds, CancellationToken cancellationToken = default);
}

internal sealed class UserProfileClient(HttpClient client, ILogger<UserProfileClient> logger) : IUserProfileClient
{
    private const string BasePath= "v1/user-profiles";
    public async Task<UserProfile?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await client.GetAsync($"{BasePath}/{id}", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<UserProfile>(
                cancellationToken: cancellationToken);
        }

        if(response.StatusCode == HttpStatusCode.NotFound)
            return null;
        
        logger.LogError("Failed to get user profile. StatusCode: {ResponseStatusCode}", response.StatusCode);
        throw new ApiResponseException(response.StatusCode, (await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken))!);
    }

    public async Task<IPagedDataSet<UserProfile>> GetAsync(int page, int perPage = 10, string? filter = null, string? order = null,
        CancellationToken cancellationToken = default)
    {
        var query = $"page={page}&recordsPerPage={perPage}";
        if(!string.IsNullOrEmpty(filter)) query += $"&filter={filter}";
        if(!string.IsNullOrEmpty(order)) query += $"&order={order}";
        
        logger.LogInformation("About to query user profiles. Base URL: {BaseAddress}/{Url}, Page: {Page}, PerPage: {RecordsPerPage}, Filter: {Filter}, Order: {Order}", client.BaseAddress, $"{BasePath}?{query}", page, perPage, filter, order);
        
        var response = await client.GetAsync($"{BasePath}?{query}", cancellationToken);
        if(response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<PagedDataSet<UserProfile>>(cancellationToken))!;
        
        logger.LogError("Failed to get user profiles. StatusCode: {ResponseStatusCode}", response.StatusCode);
        throw new ApiResponseException(response.StatusCode, (await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken))!);
    }

    public async Task<UserProfile> CreateAsync(CreateUserProfileRequest model, CancellationToken cancellationToken = default)
    {
        var response = await client.PostAsJsonAsync(BasePath, model, cancellationToken);
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<UserProfile>(cancellationToken: cancellationToken))!;
        
        logger.LogError("Failed to create user profile. StatusCode: {ResponseStatusCode}", response.StatusCode);
        throw new ApiResponseException(response.StatusCode, (await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken))!);
    }

    public async Task UpdateAsync(string id, UpdateUserProfileRequest model, CancellationToken cancellationToken = default)
    {
        var response = await client.PutAsJsonAsync($"{BasePath}/{id}", model, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to update user profile '{ID}'. StatusCode: {ResponseStatusCode}", id, response.StatusCode);
            throw new ApiResponseException(response.StatusCode, (await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken))!);
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await client.DeleteAsync($"{BasePath}/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to delete user profile '{ID}'. StatusCode: {ResponseStatusCode}", id, response.StatusCode);
            throw new ApiResponseException(response.StatusCode, (await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken))!);
        }
    }

    public async Task UpdateRoles(string id, Ulid[] roleIds, CancellationToken cancellationToken = default)
    {
        var response = await client.PutAsJsonAsync($"{BasePath}/{id}/roles", roleIds, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to update user profile roles '{ID}'. StatusCode: {ResponseStatusCode}", id, response.StatusCode);
            throw new ApiResponseException(response.StatusCode, (await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken))!);
        }
    }
}