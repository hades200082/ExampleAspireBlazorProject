namespace Presentation.Blazor.Client.Services.Api;

internal interface IApiDeleteClient<TId>
{
    Task DeleteAsync(TId id, CancellationToken cancellationToken = default);
}