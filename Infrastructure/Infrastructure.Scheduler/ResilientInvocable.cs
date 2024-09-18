using Medallion.Threading;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Infrastructure.Scheduler;

public abstract class ResilientInvocable : Invocable
{
    private readonly AsyncRetryPolicy _retryPolicy;
    protected ResilientInvocable(ILogger logger, IDistributedLockProvider lockProvider) : base(logger, lockProvider)
    {
        _retryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
    
    public override async Task Invoke()
    {
        await using var handle = await LockProvider.TryAcquireLockAsync($"Scheduler-{GetType().Name}");
        if (handle is not null)
        {
            Logger.LogInformation("Invoking resilient task '{TaskName}'", GetType().Name);
            await _retryPolicy.ExecuteAsync(async () => await InvokeAsync());
        }
    }
}