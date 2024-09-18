using System.Reflection;
using Application.Services;
using Coravel.Scheduling.Schedule.Interfaces;
using Domain.Scheduler.Tasks;
using Domain.Startup;
using Infrastructure.Authentication;
using Infrastructure.Scheduler;
using Serilog;
using Shared.Enums;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddSerilog((services, cfg) => cfg
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.AddServiceDefaults()
        .AddApplicationServices(ApplicationTypes.Worker)
        .AddScheduler(Assembly.Load("Domain.Scheduler.Tasks"))
        .AddAuthenticationOptions();

    // TODO : Add project-specific DI

    var host = builder.Build();
    await host.AwaitDatabaseReadiness(false);

    host.UseScheduler();

    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}