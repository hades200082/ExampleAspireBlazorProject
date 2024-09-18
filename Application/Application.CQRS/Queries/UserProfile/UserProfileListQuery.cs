using Domain.Services;
using Gridify;
using Infrastructure.EntityFrameworkCore;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Abstractions;
using Shared.Enums;

namespace Application.CQRS.Queries.UserProfile;

public interface UserProfileListQuery : Request<IObjectResult<IPagedDataSet<UserProfileSummaryDto>>>
{
    const string ListFailed = "UserProfile.List.Fail";
    public int Page { get; set; }
    public int RecordsPerPage { get; set; }
    public string? Filter { get; set; }
    public string? OrderBy { get; set; }
}

public class UserProfileListQueryHandler(
    AppDbContext db,
    DatabaseOptions options,
    ILogger<UserProfileListQueryHandler> logger
) : IConsumer<UserProfileListQuery>
{
    public async Task Consume(ConsumeContext<UserProfileListQuery> context)
    {
        try
        {
            IQueryable<Domain.Entities.UserProfile> query = db.UserProfiles;

            if(!string.IsNullOrEmpty(context.Message.Filter))
                query = query.ApplyFiltering(context.Message.Filter);
            
            if (options.DatabaseProvider == DatabaseProviders.CosmosDB)
                query = query.WithPartitionKey(nameof(Domain.Entities.UserProfile));
            
            var totalCount = await query.CountAsync();

            var orderBy = string.IsNullOrEmpty(context.Message.OrderBy)
                ? "Email desc"
                : context.Message.OrderBy;
            
            var data = await query
                .ApplyPaging(context.Message.Page, context.Message.RecordsPerPage)
                .ApplyOrdering(orderBy)
                .Where(x => x.DeletedAt == null)
                .Select(e => new UserProfileSummaryDto(e))
                .ToListAsync();
            
            await context.RespondAsync(
                new ObjectResult<IPagedDataSet<UserProfileSummaryDto>>(new PagedDataSet<UserProfileSummaryDto>(
                    data, 
                    totalCount, 
                    context.Message.Page, 
                    (int)Math.Ceiling((decimal)totalCount / (decimal)context.Message.RecordsPerPage))
                ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "List UserProfiles failed");
            await context.RespondAsync(
                new ValueResult<bool>(UserProfileListQuery.ListFailed, "A problem was encountered while retrieving the user profile list"));
        }
    }
}