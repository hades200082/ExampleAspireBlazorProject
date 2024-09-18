using Microsoft.AspNetCore.Mvc;

namespace Application.Host.Api.Controllers;

/// <summary>
/// Provides a base class for ASP.NET Core MVC controllers with additional functionality.
/// </summary>
/// <remarks>
/// This abstract class extends Microsoft.AspNetCore.Mvc.ControllerBase and adds custom methods
/// for handling specific HTTP status codes and error responses.
/// </remarks>
public abstract class ControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
{
    /// <summary>
    /// Returns a ProblemDetails result with a 500 error
    /// </summary>
    /// <param name="title"></param>
    /// <param name="errorCode"></param>
    /// <param name="endpointPath"></param>
    /// <returns></returns>
    [NonAction]
    public ObjectResult Problem400(string title, string errorCode, string endpointPath)
    {
        return Problem(
            errorCode,
            $"{endpointPath}?{Environment.MachineName}+{Environment.ProcessId}",
            StatusCodes.Status400BadRequest,
            title,
            "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/400"
        );
    }
    
    /// <summary>
    /// Returns a ProblemDetails result with a 500 error
    /// </summary>
    /// <param name="title"></param>
    /// <param name="errorCode"></param>
    /// <param name="endpointPath"></param>
    /// <returns></returns>
    [NonAction]
    public ObjectResult Problem500(string title, string errorCode, string endpointPath)
    {
        return Problem(
            errorCode,
            $"{endpointPath}?{Environment.MachineName}+{Environment.ProcessId}",
            StatusCodes.Status500InternalServerError,
            title,
            "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/500"
        );
    }
}