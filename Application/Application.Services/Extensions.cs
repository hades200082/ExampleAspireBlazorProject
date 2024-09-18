using System.Reflection;
using Domain.Services;
using Infrastructure.Authentication;
using Infrastructure.MassTransit;
using Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Shared.Enums;

namespace Application.Services;

public static class Extensions
{
    public static IHostApplicationBuilder AddApplicationServices(this IHostApplicationBuilder builder, ApplicationTypes applicationType)
    {
        builder.AddDomainServices(applicationType)
            .AddAppDbContext()
            .AddMassTransitMediator(Assembly.Load("Application.CQRS"))
            .AddStorage();
        
        var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
        var hasRedis = !string.IsNullOrEmpty(redisConnectionString);
        if (hasRedis)
        {
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration.GetConnectionString("Redis");
                options.InstanceName = "TemplateProject:API:";
            });
        }
        else
        {
            // Use in memory cache if we don't have redis
            Log.Warning("Redis not available - Distributed cache falling back to non-distributed memory store");
            builder.Services.AddDistributedMemoryCache();
        }
        
        if (applicationType.HasFlag(ApplicationTypes.Worker))
            builder.AddAmqpWithConsumers();
        else
            builder.AddAmqpWithoutConsumers();
        
        return builder;
    }
}