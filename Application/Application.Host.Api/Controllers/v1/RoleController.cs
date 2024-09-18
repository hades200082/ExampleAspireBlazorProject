using System.ComponentModel.DataAnnotations;
using Application.CQRS;
using Application.CQRS.Commands.Role;
using Application.CQRS.Queries.Role;
using Application.CQRS.Queries.UserProfile;
using Application.Host.Api.Models.Requests.Role;
using Application.Host.Api.Models.Responses.Role;
using Application.Host.Api.RateLimiting;
using Asp.Versioning;
using Farfetch.LoadShedding.AspNetCore.Attributes;
using Farfetch.LoadShedding.Tasks;
using MassTransit.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shared.Abstractions;
using Swashbuckle.AspNetCore.Annotations;

namespace Application.Host.Api.Controllers.v1;

/// <summary>
/// Controller for managing roles
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("v{version:apiVersion}/[controller]")]
public class RoleController(
    IMediator mediator
) : ControllerBase
{
    /// <summary>
    /// List roles
    /// </summary>
    /// <remarks>Returns a paginated list of roles and their permissions.</remarks>
    /// <param name="page">The page number to return. The first page is 1</param>
    /// <param name="recordsPerPage">The number of items to return per page.</param>
    /// <param name="filter">A Gridify filter. <see href="https://alirezanet.github.io/Gridify/guide/filtering">Gridify docs</see></param>
    /// <param name="orderBy">A Gridify ordering statement. <see href="https://alirezanet.github.io/Gridify/guide/ordering">Gridify docs</see></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Policy = "Role:Read")]
    [EndpointPriority(Priority.Normal)]
    [EnableRateLimiting(RateLimiterPolicies.List)]
    [SwaggerResponse(StatusCodes.Status200OK, "OK", typeof(IPagedDataSet<RoleResponse>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid Request Data", typeof(ValidationProblemDetails))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal Server Error", typeof(ProblemDetails))]
    public async Task<IActionResult> List(
        [Range(1, int.MaxValue)]int page = 1,
        [Range(1,100)]int recordsPerPage = 10,
        string? filter = null,
        string? orderBy = null,
        CancellationToken cancellationToken = default
    )
    {
        var client = mediator.CreateRequestClient<RoleListQuery>();
        var response =
            (await client.GetResponse<IObjectResult<IPagedDataSet<RoleDto>>>(
                new {Page = page, RecordsPerPage = recordsPerPage, Filter = filter, OrderBy = orderBy}, cancellationToken)).Message;

        if (!response.IsSuccess)
            return Problem500(
                response.ErrorMessage!,
                response.ErrorCode!,
                "/roles"
            );

        var data = response.Object!.Items.Select(r => new RoleResponse(r));
        return Ok(new PagedDataSet<RoleResponse>(data, response.Object.TotalRecords, response.Object.CurrentPage, response.Object.TotalPages));
    }
    
    /// <summary>
    /// Get role
    /// </summary>
    /// <remarks>Returns a single role by its ID</remarks>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{id:ulid}")]
    [Authorize(Policy = "Role:Read")]
    [EndpointPriority(Priority.Normal)]
    [EnableRateLimiting(RateLimiterPolicies.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, "OK", typeof(RoleResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid Request Data", typeof(ValidationProblemDetails))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(void))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal Server Error", typeof(ProblemDetails))]
    public async Task<IActionResult> Read([Required]Ulid id, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        
        var client = mediator.CreateRequestClient<RoleQuery>();
        var response = (await client.GetResponse<IObjectResult<RoleDto>>(new
        {
            Id = id
        }, cancellationToken)).Message;

        if (!response.IsSuccess)
            if (response.ErrorCode == RoleQuery.NotFound)
                return NotFound();
            else
                return Problem500(
                    response.ErrorMessage!,
                    response.ErrorCode!,
                    $"/roles/{id}"
                );
        
        return Ok(new RoleResponse(response.Object!));
    }
    
    /// <summary>
    /// Create role
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    [Authorize(Policy = "Role:Create")]
    [EndpointPriority(Priority.Normal)]
    [EnableRateLimiting(RateLimiterPolicies.Create)]
    [SwaggerResponse(StatusCodes.Status201Created, "Created", typeof(RoleResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid Request Data", typeof(ValidationProblemDetails))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal Server Error", typeof(ProblemDetails))]
    public async Task<IActionResult> Create(CreateRoleRequest model, CancellationToken cancellationToken = default)
    {
        // If a Role already exists with this name then we don't want to create another one. 
        var existsClient = mediator.CreateRequestClient<RoleExistsQuery>();
        var exists = (await existsClient.GetResponse<IValueResult<bool>>(new {model.Name}, cancellationToken)).Message;
        if (exists is {IsSuccess: true, Value: true})
            ModelState.AddModelError(nameof(CreateRoleRequest.Name), "A role with this name already exists");
        
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        
        var createClient = mediator.CreateRequestClient<CreateRoleCommand>();
        var response = (await createClient.GetResponse<IObjectResult<RoleDto>>(new
        {
            Name = model.Name,
            Description = model.Description
        }, cancellationToken)).Message;

        if (!response.IsSuccess)
            return Problem500(
                response.ErrorMessage!,
                response.ErrorCode!,
                "/roles"
            );

        return Created(Url.Action("Read", new {response.Object!.Id}),
            new RoleResponse(response.Object));
    }
    
    /// <summary>
    /// Update role
    /// </summary>
    /// <remarks>Updates the given role's data. Does not include updating permissions <see cref="Update(System.Ulid,string[],System.Threading.CancellationToken)"/></remarks>
    /// <param name="id">The role ID</param>
    /// <param name="model">The new role data</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:ulid}")]
    [Authorize(Policy = "Role:Update")]
    [EndpointPriority(Priority.Normal)]
    [EnableRateLimiting(RateLimiterPolicies.Update)]
    [SwaggerResponse(StatusCodes.Status200OK, "Ok", typeof(void))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid Request Data", typeof(ValidationProblemDetails))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid Request Data", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(void))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal Server Error", typeof(ProblemDetails))]
    public async Task<IActionResult> Update([Required]Ulid id, UpdateRoleRequest model, CancellationToken cancellationToken = default)
    {
        // If a Role already exists with this name then we don't want to create another one. 
        var existsClient = mediator.CreateRequestClient<RoleExistsOtherQuery>();
        var exists = (await existsClient.GetResponse<IValueResult<bool>>(new { ID = id, model.Name}, cancellationToken)).Message;
        if (exists is {IsSuccess: true, Value: true})
            ModelState.AddModelError(nameof(UpdateRoleRequest.Name), "Another role with this name already exists");
        
        // Is the passed in model valid
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        
        var updateClient = mediator.CreateRequestClient<UpdateRoleCommand>();
        var response = (await updateClient.GetResponse<IValueResult<bool>>(new
        {
            Id = id,
            Name = model.Name,
            Description = model.Description
        }, cancellationToken)).Message;

        if (!response.IsSuccess)
            if (response.ErrorCode == UpdateRoleCommand.Locked)
                return Problem400(response.ErrorMessage!, response.ErrorCode!, $"/roles/{id}");
            else if (response.ErrorCode == UpdateRoleCommand.NotFound)
                return NotFound();
            else
                return Problem500(response.ErrorMessage!,response.ErrorCode!,$"/roles/{id}");

        return Ok();
    }
    
    /// <summary>
    /// Update role permissions
    /// </summary>
    /// <remarks>Updates a role's permissions</remarks>
    /// <param name="id">The role ID to update</param>
    /// <param name="permissions">The new permissions for the role</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:ulid}/permissions")]
    [Authorize(Policy = "Role:ManagePermissions")]
    [EndpointPriority(Priority.Normal)]
    [EnableRateLimiting(RateLimiterPolicies.Update)]
    [SwaggerResponse(StatusCodes.Status200OK, "Ok", typeof(void))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid Request Data", typeof(ValidationProblemDetails))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid Request Data", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(void))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal Server Error", typeof(ProblemDetails))]
    public async Task<IActionResult> Update([Required]Ulid id, [Required]string[] permissions, CancellationToken cancellationToken = default)
    {
        // Is the passed in model valid
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        
        var updateClient = mediator.CreateRequestClient<UpdateRolePermissionsCommand>();
        var response = (await updateClient.GetResponse<IValueResult<bool>>(new
        {
            Id = id,
            Permissions = permissions
        }, cancellationToken)).Message;

        if (!response.IsSuccess)
            if (response.ErrorCode == UpdateRolePermissionsCommand.Locked)
                return Problem400(response.ErrorMessage!, response.ErrorCode!, $"/roles/{id}");
            else if (response.ErrorCode == UpdateRolePermissionsCommand.NotFound)
                return NotFound();
            else
                return Problem500(response.ErrorMessage!,response.ErrorCode!,$"/roles/{id}");

        return Ok();
    }
    
    /// <summary>
    /// Delete role
    /// </summary>
    /// <remarks>Deletes the given role. Deleted roles are automatically removed from users.</remarks>
    /// <param name="id">The Role ID to delete</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpDelete("{id:ulid}")]
    [Authorize(Policy = "Role:Delete")]
    [EndpointPriority(Priority.Normal)]
    [EnableRateLimiting(RateLimiterPolicies.Delete)]
    [SwaggerResponse(StatusCodes.Status200OK, "Ok", typeof(void))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid Request Data", typeof(ValidationProblemDetails))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid Request Data", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(void))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal Server Error", typeof(ProblemDetails))]
    public async Task<IActionResult> Delete([Required]Ulid id, CancellationToken cancellationToken = default)
    {
        // Is the passed in model valid
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        
        var deleteclient = mediator.CreateRequestClient<DeleteRoleCommand>();
        var response = (await deleteclient.GetResponse<IValueResult<bool>>(new
        {
            Id = id
        }, cancellationToken)).Message;

        if (!response.IsSuccess)
            if (response.ErrorCode == DeleteRoleCommand.Locked)
                return Problem400(response.ErrorMessage!, response.ErrorCode!, $"/roles/{id}");
            else if (response.ErrorCode == DeleteRoleCommand.NotFound)
                return NotFound();
            else
                return Problem500(response.ErrorMessage!,response.ErrorCode!,$"/roles/{id}");

        return Ok();
    }
}