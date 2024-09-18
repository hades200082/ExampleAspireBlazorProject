using Domain.Services;
using Gridify;
using Infrastructure.EntityFrameworkCore;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Abstractions;
using Shared.Enums;

namespace Application.CQRS.Queries.Role;

public interface RoleListQuery : Request<IObjectResult<IPagedDataSet<RoleDto>>>
{
    const string ListFailed = "Role.List.Fail";
    public int Page { get; set; }
    public int RecordsPerPage { get; set; }
    public string? Filter { get; set; }
    public string? OrderBy { get; set; }
}

public class RoleListQueryHandler(
    AppDbContext db,
    DatabaseOptions options,
    ILogger<RoleListQueryHandler> logger
) : IConsumer<RoleListQuery>
{
    public async Task Consume(ConsumeContext<RoleListQuery> context)
    {
        try
        {
            IQueryable<Domain.Entities.Role> query = db.Roles;

            if(!string.IsNullOrEmpty(context.Message.Filter))
                query = query.ApplyFiltering(context.Message.Filter);

            if (options.DatabaseProvider == DatabaseProviders.CosmosDB)
                query = query.WithPartitionKey(nameof(Domain.Entities.UserProfile));
            
            var totalCount = await query.CountAsync();
            
            var orderBy = string.IsNullOrEmpty(context.Message.OrderBy)
                ? "Email desc"
                : context.Message.OrderBy;
            
            var data = await query
                .ApplyOrdering(orderBy)
                .ApplyPaging(context.Message.Page, context.Message.RecordsPerPage)
                .Select(e => new RoleDto(e))
                .ToListAsync();
            
            await context.RespondAsync(
                new ObjectResult<IPagedDataSet<RoleDto>>(new PagedDataSet<RoleDto>(
                    data, 
                    totalCount, 
                    context.Message.Page, 
                    (int)Math.Ceiling((decimal)totalCount / (decimal)context.Message.RecordsPerPage))
                ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "List Roles failed");
            await context.RespondAsync(
                new ValueResult<bool>(RoleListQuery.ListFailed, "A problem was encountered while retrieving the role list"));
        }
    }
}