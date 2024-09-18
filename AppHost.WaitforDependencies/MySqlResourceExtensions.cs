using AppHost.WaitforDependencies;
using Aspire.Hosting.ApplicationModel;
using HealthChecks.MySql;

namespace AppHost.Extensions;

public static class MySqlResourceExtensions
{
    /// <summary>
    /// Adds a health check to the MySQL server resource.
    /// </summary>
    public static IResourceBuilder<MySqlServerResource> WithHealthCheck(this IResourceBuilder<MySqlServerResource> builder)
    {
        return builder.WithAnnotation(HealthCheckAnnotation.Create(cs => new MySqlHealthCheck(new MySqlHealthCheckOptions(cs))));
    }

    /// <summary>
    /// Adds a health check to the MySQL database resource.
    /// </summary>
    public static IResourceBuilder<MySqlDatabaseResource> WithHealthCheck(this IResourceBuilder<MySqlDatabaseResource> builder)
    {
        return builder.WithAnnotation(HealthCheckAnnotation.Create(cs => new MySqlHealthCheck(new MySqlHealthCheckOptions(cs))));
    }
}