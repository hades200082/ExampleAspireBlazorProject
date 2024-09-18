namespace Presentation.Blazor.Client.Services.Api;

internal interface IApiCreateClient<TCreateModel, TResponseModel>
{
    Task<TResponseModel> CreateAsync(TCreateModel model, CancellationToken cancellationToken = default);
}