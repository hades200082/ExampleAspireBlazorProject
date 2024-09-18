using Domain.Services;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Commands.UserProfile;

public interface CreateUserProfileCommand : Request<IObjectResult<UserProfileCreatedDto>>
{
    const string CreateFailed = "UserProfile.Create.Fail";
    
    public string UserExternalId { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
}

public class CreateUserProfileCommandHandler(
    AppDbContext db,
    ILogger<CreateUserProfileCommandHandler> logger
) : IConsumer<CreateUserProfileCommand>
{
    public async Task Consume(ConsumeContext<CreateUserProfileCommand> context)
    {
        var entity = new Domain.Entities.UserProfile
        {
            Id = context.Message.UserExternalId,
            Email = context.Message.Email,
            Name = context.Message.Name,
        };

        try
        {
            db.UserProfiles.Add(entity);
            await db.SaveChangesAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create Userprofile failed for external ID: {ID}", entity.Id);
            await context.RespondAsync(
                new ObjectResult<UserProfileCreatedDto>(CreateUserProfileCommand.CreateFailed, "A problem was encountered while creating the user profile"));
        }

        var dto = new UserProfileCreatedDto(entity);
        await context.RespondAsync(new ObjectResult<UserProfileCreatedDto>(dto));
    }
}