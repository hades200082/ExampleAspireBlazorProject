using System.Diagnostics.CodeAnalysis;
using Coravel.Scheduling.Schedule.Interfaces;
using Domain.Services;
using Infrastructure.Authentication.Abstractions;
using Infrastructure.Scheduler;
using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Domain.Scheduler.Tasks;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class RemoveDeletedUsers(
    AppDbContext db,
    IUserManager userManager,
    ILogger<RemoveDeletedUsers> logger,
    IDistributedLockProvider lockProvider
) : ResilientInvocable(logger, lockProvider)
{
    protected override async Task InvokeAsync()
    {
        // Delete in batches of 10 for performance - task can run frequently
        // to make sure things are deleted in a timely manner
        // In reality, there is likely to only be a handful to delete at a time.
        var userIds = db.UserProfiles
            .Where(u => u.DeletedAt != null && u.DeletedAt < DateTime.UtcNow)
            .Take(10)
            .Select(u => u.Id)
            .ToList();

        foreach (var userId in userIds)
        {
            try
            {
                // TODO : Add deletion of other stuff here as needed.
                
                // We need to ensure that if we've already successfully deleted the OIDC user
                // we don't try to again as that will error. So we check if the OIDC user exists
                // first, the only way we can.
                var oidcUser = await userManager.GetUser(userId);

                if (oidcUser is not null)
                    await userManager.DeleteUser(userId);

                await db.UserProfiles
                    .Where(u => u.Id == userId)
                    .ExecuteDeleteAsync();
            }
            catch(Exception ex)
            {
                // Log and move on. Don't bork the whole process. It'll get attempted again next time.
                logger.LogError(ex, "An error occured while removing user with id {UserId}", userId);
            }
        }
        
    }

    #if DEBUG
    public override Action<IScheduler> Schedule =>
        scheduler => scheduler.Schedule<RemoveDeletedUsers>().EveryFiveMinutes().RunOnceAtStart();
    #else
    public override Action<IScheduler> Schedule =>
        scheduler => scheduler.Schedule<RemoveDeletedUsers>().EveryThirtyMinutes();
    #endif
    
}