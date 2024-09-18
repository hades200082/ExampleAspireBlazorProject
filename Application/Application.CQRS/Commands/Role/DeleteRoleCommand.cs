using Domain.Services;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Commands.Role;

public interface DeleteRoleCommand : Request<IValueResult<bool>>
{
    const string NotFound = "Role.Delete.NotFound";
    const string Locked = "Role.Delete.Locked";
    const string DeleteFailed = "Role.Delete.Fail";
    
    public Ulid Id { get; set; }
}

public class DeleteUserProfileCommandHandler(
    AppDbContext db,
    ILogger<DeleteUserProfileCommandHandler> logger
) : IConsumer<DeleteRoleCommand>
{
    public async Task Consume(ConsumeContext<DeleteRoleCommand> context)
    {
        try
        {
            var entity = await db.Roles.FindAsync(context.Message.Id);

            if (entity is null)
            {
                logger.LogWarning("Delete Role failed for ID: {ID} - record not found", context.Message.Id);
                await context.RespondAsync(
                    new ValueResult<bool>(DeleteRoleCommand.NotFound, "The requested record was not found"));
                return;
            }
            
            if (entity.Locked)
            {
                logger.LogWarning("Delete Role failed for ID: {ID} - role is locked", context.Message.Id);
                await context.RespondAsync(
                    new ValueResult<bool>(DeleteRoleCommand.Locked, "System roles cannot be deleted"));
                return;
            }

            await db.Roles.Where(r => r.Id == context.Message.Id).ExecuteDeleteAsync();
            
            await context.RespondAsync(new ValueResult<bool>(true));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create Role failed for external ID: {ID}", context.Message.Id);
            await context.RespondAsync(
                new ValueResult<bool>(DeleteRoleCommand.DeleteFailed, "A problem was encountered while deleting the role"));
        }
    }
}