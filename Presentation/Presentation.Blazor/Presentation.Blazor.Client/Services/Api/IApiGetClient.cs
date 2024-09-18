using Shared.Abstractions;

namespace Presentation.Blazor.Client.Services.Api;

internal interface IApiGetClient<TId, T>
{
    Task<T?> GetAsync(TId id, CancellationToken cancellationToken = default);
    Task<IPagedDataSet<T>> GetAsync(int page, int perPage = 10, string? filter = null, string? order = null, CancellationToken cancellationToken = default);
}