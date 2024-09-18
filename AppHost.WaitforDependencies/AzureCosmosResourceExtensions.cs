using AppHost.WaitforDependencies;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using HealthChecks.CosmosDb;
using Microsoft.Azure.Cosmos;

namespace AppHost.Extensions;

public static class AzureCosmosResourceExtensions
{
    /// <summary>
    /// Adds a health check to the cosmos server resource.
    /// </summary>
    public static IResourceBuilder<AzureCosmosDBResource> WithHealthCheck(this IResourceBuilder<AzureCosmosDBResource> builder, string databaseId)
    {
        return builder.WithAnnotation(HealthCheckAnnotation.Create(cs =>
            new AzureCosmosDbHealthCheck(new CosmosClient(cs),
                new AzureCosmosDbHealthCheckOptions {DatabaseId = databaseId})));
    }
}