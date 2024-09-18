using Domain.Services;
using Infrastructure.EntityFrameworkCore;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;

namespace Application.CQRS.Queries.UserProfile;

public interface RoleExistsQuery : Request<ValueResult<bool>>
{
    public string Name { get; set; }
}

public class RoleExistsQueryHandler(
    AppDbContext db,
    DatabaseOptions dbOptions
) : IConsumer<RoleExistsQuery>
{
    public async Task Consume(ConsumeContext<RoleExistsQuery> context)
    {
        IQueryable<Domain.Entities.Role> query = db.Roles;

        if (dbOptions.DatabaseProvider == DatabaseProviders.CosmosDB)
            query = query.WithPartitionKey(nameof(Domain.Entities.UserProfile));

        var result = await query.AnyAsync(x => x.Name.ToUpper() == context.Message.Name.ToUpper());

        await context.RespondAsync(new ValueResult<bool>(result));
    }
}