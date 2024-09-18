namespace Presentation.Blazor.Client.Services.Api;

public interface IApiUpdateClient<TId, TUpdateModel>
{
    Task UpdateAsync(TId id, TUpdateModel model, CancellationToken cancellationToken = default);
}