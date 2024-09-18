using Aspire.Hosting.ApplicationModel;
using HealthChecks.Redis;

namespace AppHost.WaitforDependencies;

public static class RedisResourceHealthCheckExtensions
{
    /// <summary>
    /// Adds a health check to the Redis server resource.
    /// </summary>
    public static IResourceBuilder<RedisResource> WithHealthCheck(this IResourceBuilder<RedisResource> builder)
    {
        return builder.WithAnnotation(HealthCheckAnnotation.Create(cs => new RedisHealthCheck(cs)));
    }
}
