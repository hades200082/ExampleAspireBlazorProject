using System.ComponentModel.DataAnnotations;
using Application.CQRS;
using Application.CQRS.Commands.UserProfile;
using Application.CQRS.Queries.UserProfile;
using Application.Host.Api.Extensions;
using Application.Host.Api.Models.Requests.UserProfile;
using Application.Host.Api.Models.Responses.UserProfile;
using Application.Host.Api.RateLimiting;
using Application.Host.Api.Services;
using Asp.Versioning;
using Farfetch.LoadShedding.AspNetCore.Attributes;
using Farfetch.LoadShedding.Tasks;
using HeimGuard;
using MassTransit.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shared.Abstractions;
using Swashbuckle.AspNetCore.Annotations;

namespace Application.Host.Api.Controllers.v1;

/// <summary>
/// Controller for managing user profiles.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("v{version:apiVersion}/[controller]")]
public class UserProfileController(
    IMediator mediator,
    IHeimGuardClient permissionClient,
    IRedactionService redactionService
) : ControllerBase
{
    /// <summary>
    /// Get User Profiles
    /// </summary>
    /// <remarks>
    /// Returns a list of user profiles, paginated.
    ///
    /// If the user doesn't have UserProfile:Read permissions then we redact PII 
    /// </remarks>
    /// <param name="page">The page number to return. The first page is 1</param>
    /// <param name="recordsPerPage">The number of items to return per page.</param>
    /// <param name="filter">A Gridify filter. <see href="https://alirezanet.github.io/Gridify/guide/filtering">Gridify docs</see></param>
    /// <param name="orderBy">A Gridify ordering statement. <see href="https://alirezanet.github.io/Gridify/guide/ordering">Gridify docs</see></param>
    /// <returns></returns>
    [Authorize]
    [HttpGet]
    [EndpointPriority(Priority.Normal)]
    [EnableRateLimiting(RateLimiterPolicies.List)]
    [SwaggerResponse(StatusCodes.Status200OK, "OK", typeof(IPagedDataSet<UserProfileResponse>))]
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
        var client = mediator.CreateRequestClient<UserProfileListQuery>();
        var response =
            (await client.GetResponse<IObjectResult<IPagedDataSet<UserProfileSummaryDto>>>(
                new {Page = page, RecordsPerPage = recordsPerPage, Filter = filter, OrderBy = orderBy}, cancellationToken)).Message;

        if (!response.IsSuccess)
            return Problem500(
                response.ErrorMessage!,
                response.ErrorCode!,
                "/user-profiles"
            );

        var data = new List<UserProfileResponse>();
        foreach (var item in response.Object.Items)
        {
            if (!(User.Identity?.IsAuthenticated ?? false) || !await permissionClient.HasPermissionAsync("UserProfile:Read"))
                data.Add(redactionService.Redact(new UserProfileResponse(item)));
            else
                data.Add(new UserProfileResponse(item));
        }
        
        return Ok(new PagedDataSet<UserProfileResponse>(data, response.Object.TotalRecords, response.Object.CurrentPage, response.Object.TotalPages));
    }

    /// <summary>
    /// Get User Profile
    /// </summary>
    /// <remarks>
    /// Returns the user profile specified by the `id` parameter.
    ///
    /// If the requested user ID is not the currently authenticated user, PII is redacted 
    /// </remarks>
    /// <param name="id">The Identity Provider's UserID</param>
    /// <returns></returns>
    [Authorize]
    [HttpGet("{id}")]
    [EndpointPriority(Priority.Normal)]
    [EnableRateLimiting(RateLimiterPolicies.Read)]
    [SwaggerResponse(StatusCodes.Status200OK, "OK", typeof(UserProfileResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid Request Data", typeof(ValidationProblemDetails))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "No user profile exists with the given ID", typeof(void))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal Server Error", typeof(ProblemDetails))]
    public async Task<IActionResult> Read(string id, CancellationToken cancellationToken = default)
    {
        var client = mediator.CreateRequestClient<UserProfileQuery>();
        var response = (await client.GetResponse<IObjectResult<UserProfileDto>>(new
        {
            ExternalId = id
        }, cancellationToken)).Message;

        if (!response.IsSuccess)
            if (response.ErrorCode == UserProfileQuery.NotFound)
                return NotFound();
            else
                return Problem500(
                    response.ErrorMessage!,
                    response.ErrorCode!,
                    $"/user-profiles/{id}"
                );

        UserProfileResponse userProfile;
        if (id != User.GetIdentity() && !await permissionClient.HasPermissionAsync("UserProfile:Read"))
            userProfile = redactionService.Redact(new UserProfileResponse(response.Object));
        else
            userProfile = new UserProfileResponse(response.Object);
        
        return Ok(userProfile);
    }
    
    /// <summary>
    /// Create UserProfile
    /// </summary>
    /// <remarks>
    /// Creates a UserProfile for the user identified by the JWT authentication.
    /// </remarks>
    /// <param name="model">Additional fields beyond those available in the JWT</param>
    /// <returns></returns>
    [HttpPost]
    [Authorize]
    [EndpointPriority(Priority.Critical)]
    [EnableRateLimiting(RateLimiterPolicies.Create)]
    [SwaggerResponse(StatusCodes.Status201Created, "Created", typeof(UserProfileResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid Request Data", typeof(void))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid Request Data", typeof(ValidationProblemDetails))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal Server Error", typeof(ProblemDetails))]
    public async Task<IActionResult> Create(CreateUserProfileRequest model, CancellationToken cancellationToken = default)
    {
        var userId = User.GetIdentity();
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest();

        // If a UserProfile already exists for this user then we don't want to create another one. 
        var existsClient = mediator.CreateRequestClient<UserProfileExistsQuery>();
        var exists = (await existsClient.GetResponse<IValueResult<bool>>(new {ExternalId = userId}, cancellationToken)).Message;
        if (exists is {IsSuccess: true, Value: true})
            ModelState.AddModelError("Id", "The logged in user already has a user profile");

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var createClient = mediator.CreateRequestClient<CreateUserProfileCommand>();
        var response = (await createClient.GetResponse<IObjectResult<UserProfileCreatedDto>>(new
        {
            ExternalId = userId,
            Name = model.Name,
            Email = model.Email,
        }, cancellationToken)).Message;

        if (!response.IsSuccess)
            return Problem500(
                response.ErrorMessage!,
                response.ErrorCode!,
                "/user-profiles"
            );

        return Created(Url.Action("Read", new {userId}),
            new UserProfileResponse(response.Object));
    }

    /// <summary>
    /// Update UserProfile
    /// </summary>
    /// <remarks>
    /// Use this endpoint when you want to update the whole user profile in one go. All fields will be validated.
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("{id}")]
    [Authorize]
    [EndpointPriority(Priority.Normal)]
    [EnableRateLimiting(RateLimiterPolicies.Update)]
    [SwaggerResponse(StatusCodes.Status200OK, "Created", typeof(void))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid Request Data", typeof(ValidationProblemDetails))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Insufficient permissions to perform this action", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "No user profile exists with the given ID", typeof(void))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal Server Error", typeof(ProblemDetails))]
    public async Task<IActionResult> Update([Required]string id, UpdateUserProfileRequest model, CancellationToken cancellationToken = default)
    {
        // If a UserProfile doesn't exist with this ID let's return a 404 
        var existsClient = mediator.CreateRequestClient<UserProfileExistsQuery>();
        var exists = (await existsClient.GetResponse<IValueResult<bool>>(new {ExternalId = id}, cancellationToken)).Message;
        if (exists is {IsSuccess: true, Value: false})
            return NotFound();
        
        // Does the current user have permission to update the user identified by `id`
        if (!await permissionClient.HasPermissionAsync("UserProfile:Update") && User.GetIdentity() != id)
            return Forbid();
        
        // Is the passed in model valid
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        
        var updateClient = mediator.CreateRequestClient<UpdateUserProfileCommand>();
        var response = (await updateClient.GetResponse<IValueResult<bool>>(new
        {
            ExternalId = id,
            Name = model.Name,
            Email = model.Email,
            // Add other fields here
        }, cancellationToken)).Message;

        if (!response.IsSuccess)
            return Problem500(
                response.ErrorMessage!,
                response.ErrorCode!,
                $"/user-profiles/{id}"
            );

        return Ok();
    }

    /// <summary>
    /// Patch UserProfile
    /// </summary>
    /// <remarks>
    /// Use this endpoint when you want to update parts of the user profile. Validation will be limited to
    /// only what can be validated in isolation relating to the fields submitted and other fields that already
    /// have values on the record.
    ///
    /// Accepts a JSONPatchDocument <see href="https://learn.microsoft.com/en-us/aspnet/core/web-api/jsonpatch"/>
    /// which describes a series of operations to perform on the record.
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="patchDoc">JSONPatchDocument <see href="https://learn.microsoft.com/en-us/aspnet/core/web-api/jsonpatch" /></param>
    /// <returns></returns>
    [HttpPatch("{id}")]
    [Authorize]
    [EndpointPriority(Priority.Normal)]
    [EnableRateLimiting(RateLimiterPolicies.Update)]
    public async Task<IActionResult> Update([Required]string id, [FromBody][Required]JsonPatchDocument<UpdateUserProfileRequest> patchDoc, CancellationToken cancellationToken = default)
    {
        // Does the current user have permission to update the user identified by `id`
        if (!await permissionClient.HasPermissionAsync("UserProfile:Update") && User.GetIdentity() != id)
            return Forbid();
        
        // Is the passed in model valid
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        
        var client = mediator.CreateRequestClient<UserProfileQuery>();
        var getResponse = (await client.GetResponse<IObjectResult<UserProfileDto>>(new
        {
            ExternalId = id
        }, cancellationToken)).Message;
        
        if (!getResponse.IsSuccess)
            if (getResponse.ErrorCode == UserProfileQuery.NotFound)
                return NotFound();
            else
                return Problem500(
                    getResponse.ErrorMessage!,
                    getResponse.ErrorCode!,
                    $"/user-profiles/{id}"
                );

        var updateModel = new UpdateUserProfileRequest
        {
            Name = getResponse.Object.Name,
            Email = getResponse.Object.Email
        };
        
        patchDoc.ApplyTo(updateModel, ModelState);
        
        // Is the new update model valid
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        
        var updateClient = mediator.CreateRequestClient<UpdateUserProfileCommand>();
        var response = (await updateClient.GetResponse<IValueResult<bool>>(new
        {
            ExternalId = id,
            Name = updateModel.Name,
            Email = updateModel.Email,
            // Add other fields here
        }, cancellationToken)).Message;

        if (!response.IsSuccess)
            return Problem500(
                response.ErrorMessage!,
                response.ErrorCode!,
                $"/user-profiles/{id}"
            );

        return Ok();
    }

    [HttpPut("{id}/roles")]
    [Authorize(Policy = "UserProfile:ManageRoles")]
    [EndpointPriority(Priority.Normal)]
    [EnableRateLimiting(RateLimiterPolicies.Update)]
    public async Task<IActionResult> UpdateRoles([Required] string id, [Required]Ulid[] roleIds,
        CancellationToken cancellationToken = default)
    {
        // If a UserProfile doesn't exist with this ID let's return a 404 
        var existsClient = mediator.CreateRequestClient<UserProfileExistsQuery>();
        var exists = (await existsClient.GetResponse<IValueResult<bool>>(new {ExternalId = id}, cancellationToken)).Message;
        if (exists is {IsSuccess: true, Value: false})
            return NotFound();
        
        // Is the passed in model valid
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        
        var updateClient = mediator.CreateRequestClient<UpdateUserProfileRolesCommand>();
        var response = (await updateClient.GetResponse<IValueResult<bool>>(new
        {
            ExternalId = id,
            RoleIds = roleIds,
            // Add other fields here
        }, cancellationToken)).Message;

        if (!response.IsSuccess)
            return Problem500(
                response.ErrorMessage!,
                response.ErrorCode!,
                $"/user-profiles/{id}/roles"
            );

        return Ok();
    }

    /// <summary>
    /// Delete user profile
    /// </summary>
    /// <param name="id">The unique identifier of the user profile to delete</param>
    /// <remarks>
    /// This endpoint deletes a user profile with the specified ID. It requires authorization and is subject to rate limiting.
    /// 
    /// The operation follows these steps:
    /// 1. Checks if the user profile exists
    /// 2. Verifies if the current user has permission to delete the profile
    /// 3. Validates the ID
    /// 4. Marks the user profile for asynchronous deletion
    /// 
    /// Once marked, the user profile will be deleted by a background process ASAP. Since this process may take some time
    /// to complete, the API returns an "Accepted" response immediately but the user should be informed that it may take
    /// a little while to complete the deletion.
    /// </remarks>
    [HttpDelete("{id}")]
    [Authorize]
    [EndpointPriority(Priority.Normal)]
    [EnableRateLimiting(RateLimiterPolicies.Delete)]
    [SwaggerResponse(StatusCodes.Status202Accepted, "Accepted", typeof(void))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid Request Data", typeof(ValidationProblemDetails))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Insufficient permissions to perform this action", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "No user profile exists with the given ID", typeof(void))]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal Server Error", typeof(ProblemDetails))]
    public async Task<IActionResult> Delete([Required]string id, CancellationToken cancellationToken = default)
    {
        var existsClient = mediator.CreateRequestClient<UserProfileExistsQuery>();
        var exists = (await existsClient.GetResponse<IValueResult<bool>>(new {ExternalId = id}, cancellationToken)).Message;
        if (exists is {IsSuccess: true, Value: false})
            return NotFound();
        
        // Does the current user have permission to delete the user identified by `id`
        if (!await permissionClient.HasPermissionAsync("UserProfile:Delete") && User.GetIdentity() != id)
            return Forbid();
        
        // Is the passed in model valid
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        
        var deleteClient = mediator.CreateRequestClient<DeleteUserProfileCommand>();
        var response = (await deleteClient.GetResponse<IValueResult<bool>>(new
        {
            ExternalId = id
        }, cancellationToken)).Message;

        if (!response.IsSuccess)
            return Problem500(
                response.ErrorMessage!,
                response.ErrorCode!,
                $"/user-profiles/{id}"
            );

        return Accepted();
    }
}