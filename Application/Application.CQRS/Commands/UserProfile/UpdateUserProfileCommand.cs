using Domain.Services;
using Infrastructure.Authentication.Abstractions;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Commands.UserProfile;

public interface UpdateUserProfileCommand : Request<IValueResult<bool>>
{
    const string NotFound = "UserProfile.Update.NotFound";
    const string UpdateFailed = "UserProfile.Update.Fail";
    
    public string UserExternalId { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    
    // Add other fields here
}

public class UpdateUserProfileCommandHandler(
    AppDbContext db,
    ILogger<UpdateUserProfileCommandHandler> logger
) : IConsumer<UpdateUserProfileCommand>
{
    public async Task Consume(ConsumeContext<UpdateUserProfileCommand> context)
    {
        try
        {
            var entity = await db.UserProfiles.FindAsync(context.Message.UserExternalId);

            if (entity is null || entity.DeletedAt is not null)
            {
                logger.LogWarning("Update UserProfile failed for external ID: {ID} - record not found", context.Message.UserExternalId);
                await context.RespondAsync(
                    new ValueResult<bool>(UpdateUserProfileCommand.NotFound, "The requested record was not found"));
                return;
            }
            
            entity.Email = context.Message.Email;
            entity.Name = context.Message.Name;
            
            await db.SaveChangesAsync(context.CancellationToken);
            
            await context.RespondAsync(new ValueResult<bool>(true));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update Userprofile failed for external ID: {ID}", context.Message.UserExternalId);
            await context.RespondAsync(
                new ValueResult<bool>(UpdateUserProfileCommand.UpdateFailed, "A problem was encountered while updating the user profile"));
        }
    }
}