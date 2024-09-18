using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Presentation.Blazor.Client.Services.Api;

namespace Presentation.Blazor.Client.Components;

public abstract class ComponentBase<T> : ComponentBase, IDisposable
{
    [Inject] protected IDialogService DialogService { get; init; } = null!;
    [Inject] protected ISnackbar Snackbar { get; init; } = null!;
    [Inject] protected NavigationManager NavigationManager { get; init; } = null!;
    [Inject] protected ILogger<T> Logger { get; init; }
    
    [CascadingParameter]
    protected Task<AuthenticationState>? AuthenticationState { get; set; }

    private CancellationTokenSource? _cts;
    protected CancellationToken ComponentDetached => (_cts ??= new CancellationTokenSource()).Token;

    protected readonly DialogOptions GlobalDialogOptions = new DialogOptions
    {
        CloseButton = false,
        MaxWidth = MaxWidth.ExtraLarge,
        Position = DialogPosition.Center,
        BackdropClick = true,
        CloseOnEscapeKey = true
    };

    protected void HandleApiException(ApiResponseException exception)
    {
        switch (exception.StatusCode)
        {
            case HttpStatusCode.TooManyRequests:
                Snackbar.Add(
                    new MarkupString(
                        "<div><h3>Too many requests</h3><p>Please wait a few minutes and try again</p></div>"),
                    Severity.Warning);
                break;

            case HttpStatusCode.Unauthorized:
                NavigationManager.NavigateTo("authentication/logout");
                break;

            case HttpStatusCode.Forbidden:
                Snackbar.Add(
                    new MarkupString(
                        "<div><h3>Unauthorized</h3><p>you don't have permissions to do that</p><p>Your permissions may have changed since you logged in. Please log out and back in to refresh them.</p></div>")
                    , Severity.Warning);
                break;

            default:
                Snackbar.Add(exception.ProblemDetails.Detail, Severity.Error);
                break;
        }
    }

    protected async Task<bool> HasPermission(string permission)
    {
        if (AuthenticationState is null) return false;
        
        var authState = await AuthenticationState;
        return (authState.User.Identity?.IsAuthenticated ?? false) 
               && authState.User.HasClaim(c => c.Type == "granted-permission" && c.Value == permission); 
    }

    public virtual void Dispose()
    {
        if (_cts is null) return;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
        GC.SuppressFinalize(this);
    }
}