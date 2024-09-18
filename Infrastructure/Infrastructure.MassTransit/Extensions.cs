using System.Reflection;
using MassTransit;
using MassTransit.SqlTransport.PostgreSql;
using MassTransit.SqlTransport.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Enums;

namespace Infrastructure.MassTransit;

public static class Extensions
{
    public static IHostApplicationBuilder AddMassTransitBus(this IHostApplicationBuilder builder, params Assembly[] assemblies)
    {
        var options = builder.Configuration.GetSection("AMQP").Get<TransportOptions>();

        ArgumentNullException.ThrowIfNull(options?.Transport);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ConnectionStringName);

        var connectionString = builder.Configuration.GetConnectionString(options.ConnectionStringName);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        builder.Services.AddSingleton(options);

        var endpointNameFormatter = new KebabCaseEndpointNameFormatter();
        builder.Services.AddMassTransit(cfg =>
        {
            if(assemblies.Length > 0)
                cfg.AddConsumers(assemblies);

            cfg.SetEndpointNameFormatter(endpointNameFormatter);
            
            switch (options.Transport)
            {
                case AmqpTransports.RabbitMQ:
                    cfg.UsingRabbitMq((ctx, opt) =>
                    {
                        opt.Host(connectionString);
                        
                        if(assemblies.Length > 0)
                            opt.ConfigureEndpoints(ctx);
                        
                        opt.UseEndpointDefaults(endpointNameFormatter);
                    });
                    break;
                case AmqpTransports.AzureServiceBus:
                    cfg.UsingAzureServiceBus((ctx, opt) =>
                    {
                        opt.Host(connectionString);
                        
                        if(assemblies.Length > 0)
                            opt.ConfigureEndpoints(ctx);
                        
                        opt.UseEndpointDefaults(endpointNameFormatter);
                    });
                    break;
                case AmqpTransports.AmazonSqs:
                    throw new NotImplementedException("AmazonSQS is not yet implemented");
                    break;
                case AmqpTransports.SqlServer:
                    cfg.UsingSqlServer((ctx, opt) =>
                    {
                        opt.Host(new SqlServerSqlHostSettings(connectionString));
                        
                        if(assemblies.Length > 0)
                            opt.ConfigureEndpoints(ctx);
                        
                        opt.UseEndpointDefaults(endpointNameFormatter);
                    });
                    break;
                case AmqpTransports.Postgres:
                    cfg.UsingPostgres((ctx, opt) =>
                    {
                        opt.Host(new PostgresSqlHostSettings(connectionString));
                        
                        if(assemblies.Length > 0)
                            opt.ConfigureEndpoints(ctx);
                        
                        opt.UseEndpointDefaults(endpointNameFormatter);
                    });
                    break;
                default:
                    throw new ConfigurationException("The value given for Amqp Transport is not valid");
            }
        });
        return builder;
    }

    private static void UseEndpointDefaults(this IBusFactoryConfigurator opt, IEntityNameFormatter entityNameFormatter)
    {
        opt.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1)));
        opt.MessageTopology.SetEntityNameFormatter(entityNameFormatter);
    }

    public static IHostApplicationBuilder AddMassTransitMediator(this IHostApplicationBuilder builder, params Assembly[] assemblies)
    {
        builder.Services.AddMediator(cfg =>
        {
            cfg.AddConsumers(assemblies);
        });
        return builder;
    }
}