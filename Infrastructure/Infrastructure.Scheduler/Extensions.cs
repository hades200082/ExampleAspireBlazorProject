using System.Reflection;
using Coravel;
using Coravel.Scheduling.Schedule.Interfaces;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Infrastructure.Scheduler;

public static class Extensions
{
    private static List<Type>? _invocableTypes;

    public static IHostApplicationBuilder AddScheduler(this IHostApplicationBuilder builder,
        params Assembly[] assemblies)
    {
        var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

        builder.Services.AddSingleton<IConnectionMultiplexer>(
            sp => ConnectionMultiplexer.Connect(redisConnectionString!));
        builder.Services.AddSingleton<IDistributedLockProvider>(sp =>
        {
            var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
            // Use the Redis database (0 by default)
            return new RedisDistributedSynchronizationProvider(multiplexer.GetDatabase());
        });

        builder.Services.AddScheduler();

        _invocableTypes = assemblies.SelectMany(a => a.GetTypes())
            .Where(t => typeof(Invocable).IsAssignableFrom(t) && t is {IsInterface: false, IsAbstract: false})
            .ToList();

        foreach (var invocableType in _invocableTypes)
        {
            builder.Services.AddTransient(invocableType);
        }

        return builder;
    }

    public static IHost UseScheduler(this IHost app)
    {
        ArgumentNullException.ThrowIfNull(_invocableTypes);

        var scheduler = app.Services.GetRequiredService<IScheduler>();
        var scope = app.Services.CreateScope();
        // ReSharper disable once AccessToDisposedClosure
        var instances = _invocableTypes.Select(invocableType =>
            scope.ServiceProvider.GetRequiredService(invocableType) as Invocable);
        foreach (var invocableInstance in instances)
        {
            invocableInstance?.Schedule.Invoke(scheduler);
        }

        scope.Dispose();
        _invocableTypes = null; // Clearing it to free memory as it's not needed now
        return app;
    }
}