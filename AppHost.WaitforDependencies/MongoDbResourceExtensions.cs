 using AppHost.WaitforDependencies;
 using Aspire.Hosting.ApplicationModel;
using HealthChecks.MongoDb;
 using MongoDBDatabaseResource = Aspire.Hosting.ApplicationModel.MongoDBDatabaseResource;

 namespace AppHost.Extensions;
 
 public static class MongoDbResourceExtensions
 {
     /// <summary>
     /// Adds a health check to the Mongo server resource.
     /// </summary>
     public static IResourceBuilder<MongoDBServerResource> WithHealthCheck(this IResourceBuilder<MongoDBServerResource> builder)
     {
         return builder.WithAnnotation(HealthCheckAnnotation.Create(cs => new MongoDbHealthCheck(cs)));
     }

     /// <summary>
     /// Adds a health check to the Mongo database resource.
     /// </summary>
     public static IResourceBuilder<MongoDBDatabaseResource> WithHealthCheck(this IResourceBuilder<MongoDBDatabaseResource> builder)
     {
         return builder.WithAnnotation(HealthCheckAnnotation.Create(cs => new MongoDbHealthCheck(cs)));
     }
 }