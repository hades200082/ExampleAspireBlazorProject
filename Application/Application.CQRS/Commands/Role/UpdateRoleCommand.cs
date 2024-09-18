using Domain.Services;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Commands.Role;

public interface UpdateRoleCommand : Request<IValueResult<bool>>
{
    const string NotFound = "Role.Update.NotFound";
    const string UpdateFailed = "Role.Update.Fail";
    const string Locked = "Role.Update.Locked";
    
    public Ulid Id { get; set; }
    public string Description { get; set; }
    public string Name { get; set; }
    
    // Add other fields here
}

public class UpdateRoleCommandHandler(
    AppDbContext db,
    ILogger<UpdateRoleCommandHandler> logger
) : IConsumer<UpdateRoleCommand>
{
    public async Task Consume(ConsumeContext<UpdateRoleCommand> context)
    {
        try
        {
            var entity = await db.Roles.FindAsync(context.Message.Id);

            if (entity is null)
            {
                logger.LogWarning("Update Role failed for ID: {ID} - record not found", context.Message.Id);
                await context.RespondAsync(
                    new ValueResult<bool>(UpdateRoleCommand.NotFound, "The requested record was not found"));
                return;
            }

            if (entity.Locked)
            {
                logger.LogWarning("Update Role failed for ID: {ID} - role is locked", context.Message.Id);
                await context.RespondAsync(
                    new ValueResult<bool>(UpdateRoleCommand.Locked, "System roles cannot be updated"));
                return;
            }
            
            entity.Description = context.Message.Description;
            entity.Name = context.Message.Name;
            
            await db.SaveChangesAsync(context.CancellationToken);
            await context.RespondAsync(new ValueResult<bool>(true));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update Role failed for external ID: {ID}", context.Message.Id);
            await context.RespondAsync(
                new ValueResult<bool>(UpdateRoleCommand.UpdateFailed, "A problem was encountered while updating the role"));
        }
    }
}