using Domain.Services;
using Infrastructure.EntityFrameworkCore;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;

namespace Application.CQRS.Queries.Role;

public interface RoleExistsOtherQuery : Request<ValueResult<bool>>
{
    public Ulid Id { get; set; }
    public string Name { get; set; }
}

public class RoleExistsOtherQueryHandler(
    AppDbContext db,
    DatabaseOptions dbOptions
) : IConsumer<RoleExistsOtherQuery>
{
    public async Task Consume(ConsumeContext<RoleExistsOtherQuery> context)
    {
        IQueryable<Domain.Entities.Role> query = db.Roles;

        if (dbOptions.DatabaseProvider == DatabaseProviders.CosmosDB)
            query = query.WithPartitionKey(nameof(Domain.Entities.UserProfile));

        var result = await query.AnyAsync(x => x.Id != context.Message.Id && x.Name.ToUpper() == context.Message.Name.ToUpper());

        await context.RespondAsync(new ValueResult<bool>(result));
    }
}