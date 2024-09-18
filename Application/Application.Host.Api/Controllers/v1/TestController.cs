using Application.Host.Api.Extensions;
using Application.Host.Api.RateLimiting;
using Asp.Versioning;
using Farfetch.LoadShedding.AspNetCore.Attributes;
using Farfetch.LoadShedding.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Application.Host.Api.Controllers.v1;

/// <summary>
/// Controller class for handling simple requests to show the application is running
/// and test various aspects.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("v{version:apiVersion}/[controller]")]
public class TestController : ControllerBase
{
    /// <summary>
    /// Alive check
    /// </summary>
    /// <returns>
    /// Returns an IActionResult indicating that the application is alive and responding.
    /// </returns>
    /// <remarks>
    /// This method returns a status code of 200 (OK) along with a message "Alive" to indicate
    /// that the application is running and responsive.
    /// </remarks>
    [HttpGet("alive")]
    [EndpointPriority(Priority.NonCritical)]
    [EnableRateLimiting(RateLimiterPolicies.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, "The application is alive and responding")]
    public async Task<IActionResult> AliveCheck()
    {
        await Task.Delay(Random.Shared.Next(450, 800));
        return Ok("Alive");
    }
    
    /// <summary>
    /// Alive check
    /// </summary>
    /// <returns>
    /// Returns an IActionResult indicating that the application is alive and responding.
    /// </returns>
    /// <remarks>
    /// This method returns a status code of 200 (OK) along with a message "Alive" to indicate
    /// that the application is running and responsive.
    /// </remarks>
    [HttpPut("alive")]
    [EndpointPriority(Priority.NonCritical)]
    [EnableRateLimiting(RateLimiterPolicies.Update)]
    [SwaggerResponse(StatusCodes.Status200OK, "The application is alive and responding")]
    public async Task<IActionResult> AliveUpdate()
    {
        await Task.Yield();
        return Ok("Alive");
    }

    /// <summary>
    /// Logged in check
    /// </summary>
    /// <returns>
    /// Returns an IActionResult indicating whether the user is logged in or not.
    /// </returns>
    /// <remarks>
    /// This method requires the user to be authenticated. If the user is not logged in,
    /// an Unauthorized status code (401) is returned. If the user is logged in, an OK
    /// status code (200) is returned along with a message "Logged in".
    /// </remarks>
    [Authorize]
    [HttpGet("auth")]
    [EndpointPriority(Priority.NonCritical)]
    [EnableRateLimiting(RateLimiterPolicies.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, "The request was from an authenticated user")]
    public async Task<IActionResult> LoggedInCheck()
    {
        await Task.Yield();
        return Ok(new
        {
            Message = $"Logged in as {User.GetIdentity()}",
            UserClaims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToArray()
        });
    }

    /// <summary>
    /// Rate limiter check
    /// </summary>
    /// <returns>
    /// Returns an IActionResult indicating whether the request is rate limited or not.
    /// </returns>
    /// <remarks>
    /// This method checks if the request is rate limited or not based on the rate limiter policy.
    /// If the request is not rate limited, it returns a status code of 200 (OK) along with
    /// a message "Not limited". If the request is rate limited, it returns a status code
    /// of 429 (Too Many Requests).
    /// </remarks>
    [HttpGet("rate-limiter")]
    [EndpointPriority(Priority.NonCritical)]
    [EnableRateLimiting(RateLimiterPolicies.Create)] // Using create as it's more restrictive and easier to test
    [SwaggerResponse(StatusCodes.Status200OK, "Not rate limited")]
    [SwaggerResponse(StatusCodes.Status429TooManyRequests, "Rate limited")]
    public async Task<IActionResult> RateLimiterCheck()
    {
        await Task.Yield();
        return Ok("Not limited");
    }
}