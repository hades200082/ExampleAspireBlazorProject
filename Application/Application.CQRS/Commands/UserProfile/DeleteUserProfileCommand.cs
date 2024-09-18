using Domain.Services;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Commands.UserProfile;

public interface DeleteUserProfileCommand : Request<IValueResult<bool>>
{
    const string NotFound = "UserProfile.Delete.NotFound";
    const string DeleteFailed = "UserProfile.Delete.Fail";
    
    public string UserExternalId { get; set; }
}

public class DeleteUserProfileCommandHandler(
    AppDbContext db,
    ILogger<DeleteUserProfileCommandHandler> logger
) : IConsumer<DeleteUserProfileCommand>
{
    public async Task Consume(ConsumeContext<DeleteUserProfileCommand> context)
    {
        try
        {
            var entity = await db.UserProfiles.FindAsync(context.Message.UserExternalId);

            if (entity is null)
            {
                logger.LogWarning("Delete UserProfile failed for external ID: {ID} - record not found", context.Message.UserExternalId);
                await context.RespondAsync(
                    new ValueResult<bool>(DeleteUserProfileCommand.NotFound, "The requested record was not found"));
                return;
            }
            
            // Remove it from queries immediately
            entity.DeletedAt = DateTime.UtcNow.AddSeconds(-1); // 1 second ago to ensure things are picked up correctly in race conditions
            await db.SaveChangesAsync();

            await context.RespondAsync(new ValueResult<bool>(true));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create Userprofile failed for external ID: {ID}", context.Message.UserExternalId);
            await context.RespondAsync(
                new ValueResult<bool>(DeleteUserProfileCommand.DeleteFailed, "A problem was encountered while deleting the user profile"));
        }
    }
}