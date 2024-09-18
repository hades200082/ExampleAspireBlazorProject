using Polly;
using Polly.Retry;

namespace Shared.Resilience;

public static class PollyRetryPolicies
{
    private static readonly RetryStrategyOptions DatabaseStartupStrategy = new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder().Handle<Exception>(),
        BackoffType = DelayBackoffType.Exponential,
        MaxRetryAttempts = 30,
        Delay = TimeSpan.FromSeconds(1),
        MaxDelay = TimeSpan.FromSeconds(10)
    };

    public static readonly ResiliencePipeline DatabaseStartupResiliencePipeline = new ResiliencePipelineBuilder()
        .AddRetry(DatabaseStartupStrategy).Build();


    private static readonly RetryStrategyOptions RetryForeverWhenTrueStrategy = new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder().HandleResult(true), // if a value of true is passed in, keep retrying
        BackoffType = DelayBackoffType.Linear,
        Delay = TimeSpan.FromSeconds(5),
        MaxDelay = TimeSpan.FromSeconds(30),
        MaxRetryAttempts = int.MaxValue
    };

    public static readonly ResiliencePipeline RetryForeverWhenTruePipeline = new ResiliencePipelineBuilder().AddRetry(RetryForeverWhenTrueStrategy).Build();
}