using Domain.Services;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Commands.Role;

public interface UpdateRolePermissionsCommand : Request<IValueResult<bool>>
{
    const string NotFound = "Role.Update.NotFound";
    const string UpdateFailed = "Role.Update.Fail";
    const string Locked = "Role.Update.Locked";
    
    public Ulid Id { get; set; }
    public string[] Permissions { get; set; }
}

public class UpdateRolePermissionsCommandHandler(
    AppDbContext db,
    ILogger<UpdateRolePermissionsCommandHandler> logger
) : IConsumer<UpdateRolePermissionsCommand>
{
    public async Task Consume(ConsumeContext<UpdateRolePermissionsCommand> context)
    {
        try
        {
            var entity = await db.Roles.FindAsync(context.Message.Id);

            if (entity is null)
            {
                logger.LogWarning("Update Role failed for ID: {ID} - record not found", context.Message.Id);
                await context.RespondAsync(
                    new ValueResult<bool>(UpdateRolePermissionsCommand.NotFound, "The requested record was not found"));
                return;
            }

            if (entity.Locked)
            {
                logger.LogWarning("Update Role failed for ID: {ID} - role is locked", context.Message.Id);
                await context.RespondAsync(
                    new ValueResult<bool>(UpdateRolePermissionsCommand.Locked, "System roles cannot be updated"));
                return;
            }
            
            entity.Permissions = context.Message.Permissions;
            
            await db.SaveChangesAsync(context.CancellationToken);
            await context.RespondAsync(new ValueResult<bool>(true));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update Role failed for external ID: {ID}", context.Message.Id);
            await context.RespondAsync(
                new ValueResult<bool>(UpdateRolePermissionsCommand.UpdateFailed, "A problem was encountered while updating the role"));
        }
    }
}