using Application.Host.Api.RateLimiting;
using Asp.Versioning;
using Farfetch.LoadShedding.AspNetCore.Attributes;
using Farfetch.LoadShedding.Tasks;
using Infrastructure.Authentication.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Application.Host.Api.Controllers.v1;

/// <summary>
/// Controller for managing roles
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("v{version:apiVersion}/[controller]")]
public class PermissionController(
    IPermissionProvider permissionProvider
) : ControllerBase
{
    /// <summary>
    /// Get all permissions
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    [HttpGet]
    [EndpointPriority(Priority.Normal)]
    [EnableRateLimiting(RateLimiterPolicies.List)]
    [SwaggerResponse(StatusCodes.Status200OK, "Created", typeof(string[]))]
    public async Task<IActionResult> List(CancellationToken cancellationToken = default)
    {
        return Ok(await permissionProvider.GetAllPermissions(cancellationToken));
    }
}