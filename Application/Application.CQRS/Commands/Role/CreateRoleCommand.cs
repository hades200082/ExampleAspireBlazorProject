using Application.CQRS.Commands.UserProfile;
using Application.CQRS.Queries.Role;
using Domain.Services;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.Extensions.Logging;

namespace Application.CQRS.Commands.Role;

public interface CreateRoleCommand : Request<IObjectResult<RoleDto>>
{
    const string CreateFailed = "Role.Create.Fail";
    
    public string Description { get; set; }
    public string Name { get; set; }
}

public class CreateUserProfileCommandHandler(
    AppDbContext db,
    ILogger<CreateUserProfileCommandHandler> logger
) : IConsumer<CreateRoleCommand>
{
    public async Task Consume(ConsumeContext<CreateRoleCommand> context)
    {
        var entity = new Domain.Entities.Role
        {
            Description = context.Message.Description,
            Name = context.Message.Name,
        };

        try
        {
            db.Roles.Add(entity);
            await db.SaveChangesAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create Userprofile failed for external ID: {ID}", entity.Id);
            await context.RespondAsync(
                new ObjectResult<UserProfileCreatedDto>(CreateRoleCommand.CreateFailed, "A problem was encountered while creating the user profile"));
        }

        var dto = new RoleDto(entity);
        await context.RespondAsync(new ObjectResult<RoleDto>(dto));
    }
}