using Domain.Services;
using Infrastructure.Authentication.Abstractions;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Queries.UserProfile;

public interface UserProfileQuery : Request<IObjectResult<UserProfileDto>>
{
    const string NotFound = "UserProfile.Read.NotFound";
    const string ReadFailed = "UserProfile.Read.Fail";
    
    public string ExternalId { get; set; }
}

public class UserProfileQueryHandler(
    AppDbContext db,
    IPermissionProvider permissionProvider,
    ILogger<UserProfileQueryHandler> logger
) : IConsumer<UserProfileQuery>
{
    public async Task Consume(ConsumeContext<UserProfileQuery> context)
    {
        try
        {
            var entity = await db.UserProfiles.FindAsync(context.Message.ExternalId, context.CancellationToken);
            
            if (entity is null || entity.DeletedAt is not null)
            {
                logger.LogWarning("Select UserProfile failed for external ID: {ID} - record not found", context.Message.ExternalId);
                await context.RespondAsync(
                    new ValueResult<bool>(UserProfileQuery.NotFound, "The requested record was not found"));
                return;
            }

            await db.Entry(entity).Collection(x => x.Roles).LoadAsync(context.CancellationToken); // populate the user's roles

            string[] permissions = [];
            if (entity.Roles.Any())
                permissions = await permissionProvider.GetUserPermissions(entity);
            
            await context.RespondAsync(new ObjectResult<UserProfileDto>(new UserProfileDto(entity, permissions)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Select UserProfile failed for external ID: {ID}", context.Message.ExternalId);
            await context.RespondAsync(
                new ValueResult<bool>(UserProfileQuery.ReadFailed, "A problem was encountered while retrieving the user profile"));
        }
    }
}