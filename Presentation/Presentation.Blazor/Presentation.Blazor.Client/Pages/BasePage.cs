using Microsoft.AspNetCore.Authorization;
using Presentation.Blazor.Client.Components;

namespace Presentation.Blazor.Client.Pages;

[Authorize]
public abstract partial class BasePage<T> : ComponentBase<T>
{
    
    
}