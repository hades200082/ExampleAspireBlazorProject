using Coravel.Invocable;
using Coravel.Scheduling.Schedule.Interfaces;
using Medallion.Threading;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Scheduler;

public abstract class Invocable(ILogger logger, IDistributedLockProvider lockProvider) : IInvocable
{
    protected ILogger Logger { get; } = logger;
    public IDistributedLockProvider LockProvider { get; } = lockProvider;

    public abstract Action<IScheduler> Schedule { get; }
    
    protected abstract Task InvokeAsync();
    public virtual async Task Invoke()
    {
        await using var handle = await LockProvider.TryAcquireLockAsync($"Scheduler-{GetType().Name}");
        if (handle is not null)
        {
            Logger.LogInformation("Invoking task '{TaskName}'", GetType().Name);
            await InvokeAsync();
        }
    }
}