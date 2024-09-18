using System.Reflection;
using Infrastructure.Authentication.Abstractions;
using Infrastructure.Email;
using Infrastructure.EntityFrameworkCore;
using Infrastructure.MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Enums;

namespace Domain.Services;

public static class Extensions
{
    public static IHostApplicationBuilder AddDomainServices(this IHostApplicationBuilder builder,
        ApplicationTypes applicationType)
    {
        builder.AddEmailSender();
        builder.Services.AddTransient<IRoleProvider, RoleProvider>();
        builder.Services.AddTransient<IPermissionProvider, PermissionProvider>();
        
        /* Change this as needed for another provider */
        builder.Services.AddSingleton<IEmailTemplateProvider, BlobStorageEmailTemplateProvider>();
        // builder.Services.AddSingleton<IEmailTemplateProvider, DatabaseEmailTemplateProvider>();

        return builder;
    }

    public static IHostApplicationBuilder AddAppDbContext(this IHostApplicationBuilder builder)
    {
        builder.AddConfiguredDbContext<AppDbContext>();
        return builder;
    }

    public static IHostApplicationBuilder AddAmqpWithConsumers(this IHostApplicationBuilder builder)
    {
        builder.AddMassTransitBus(Assembly.Load("Domain.AMQP.Consumers"));
        return builder;
    }

    public static IHostApplicationBuilder AddAmqpWithoutConsumers(this IHostApplicationBuilder builder)
    {
        builder.AddMassTransitBus();
        return builder;
    }
}