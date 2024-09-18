using Domain.Services;
using Infrastructure.Authentication.Abstractions;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Queries.Role;

public interface RoleQuery : Request<IObjectResult<RoleDto>>
{
    const string NotFound = "Role.Read.NotFound";
    const string ReadFailed = "Role.Read.Fail";
    
    public Ulid Id { get; set; }
}

public class RoleQueryHandler(
    AppDbContext db,
    ILogger<RoleQueryHandler> logger
) : IConsumer<RoleQuery>
{
    public async Task Consume(ConsumeContext<RoleQuery> context)
    {
        try
        {
            var entity = await db.Roles.FindAsync(context.Message.Id);
            
            if (entity is null)
            {
                logger.LogWarning("Select Role failed for external ID: {ID} - record not found", context.Message.Id);
                await context.RespondAsync(
                    new ObjectResult<RoleDto>(RoleQuery.NotFound, "The requested record was not found"));
                return;
            }

            await context.RespondAsync(new ObjectResult<RoleDto>(new RoleDto(entity)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Select Role failed for external ID: {ID}", context.Message.Id);
            await context.RespondAsync(
                new ObjectResult<RoleDto>(RoleQuery.ReadFailed, "A problem was encountered while retrieving the role"));
        }
    }
}