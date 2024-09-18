using Domain.Services;
using Infrastructure.EntityFrameworkCore;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;

namespace Application.CQRS.Queries.UserProfile;

public interface UserProfileExistsQuery : Request<ValueResult<bool>>
{
    public string ExternalId { get; set; }
}

public class UserProfileExistsQueryHandler(
    AppDbContext db,
    DatabaseOptions dbOptions
) : IConsumer<UserProfileExistsQuery>
{
    public async Task Consume(ConsumeContext<UserProfileExistsQuery> context)
    {
        var query = db.UserProfiles
            .Where(u => u.DeletedAt == null);

        if (dbOptions.DatabaseProvider == DatabaseProviders.CosmosDB)
            query = query.WithPartitionKey(nameof(Domain.Entities.UserProfile));

        var result = await query.AnyAsync(x => x.Id == context.Message.ExternalId);

        await context.RespondAsync(new ValueResult<bool>(result));
    }
}