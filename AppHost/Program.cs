using AppHost.Extensions;
using AppHost.Options;
using AppHost.WaitforDependencies;
using Microsoft.Extensions.Configuration;
using Shared.Enums;

var builder = DistributedApplication.CreateBuilder(args);

#region Fetch & Validate Options

var keycloakAdminPassword = builder.AddParameter("KeycloakAdminPassword", true);
var databasePassword = builder.AddParameter("DatabasePassword", true);
var dbOptions = builder.Configuration.GetRequiredSection("Database").Get<DatabaseOptions>();
var amqpOptions = builder.Configuration.GetRequiredSection("AMQP").Get<AmqpOptions>();
var storageOptions = builder.Configuration.GetRequiredSection("Storage").Get<StorageOptions>();
var presentationOptions = builder.Configuration.GetRequiredSection("Presentation").Get<PresentationOptions>();

ArgumentNullException.ThrowIfNull(dbOptions);
ArgumentNullException.ThrowIfNull(amqpOptions);
ArgumentNullException.ThrowIfNull(storageOptions);
ArgumentNullException.ThrowIfNull(presentationOptions);

#endregion

var db = builder.AddDatabase(
    dbOptions.DatabaseProvider,
    dbOptions.ConnectionStringName,
    databasePassword);

var amqp = builder.AddAmqp(amqpOptions, databasePassword);

var storage = builder.AddStorageProvider(storageOptions);

var redis = builder.AddRedis("Redis")
    .WithDataVolume()
    .WithHealthCheck()
    .WithRedisCommander();

/*
 * The default realm import is pre-configured with the correct clients and users
 * for the project but the realm name needs changing for each project.
 *
 * Be sure to run the appropriate SetProjectName script as documented at
 * https://template-project.wildkatz.org/en/getting-started#set-the-project-name
 * before running the AppHost for the first time.
 *
 * If you have already run the AppHost, you will need to delete the docker volume
 * it created so that it can import again.
 */
var keycloak = builder.AddKeycloak("Keycloak", 8080, adminPassword: keycloakAdminPassword)
    .WithDataVolume()
    .WithRealmImport("../.keycloak/import", true)
    .WithHealthCheck("http", "admin/master/console");

var enableLoadShedding = builder.Configuration.GetValue<bool>("Features:EnableLoadShedding");
var enableRateLimiting = builder.Configuration.GetValue<bool>("Features:EnableRateLimiting");

var api = builder.AddProject<Projects.Application_Host_Api>("TemplateProject-Api")
        .WithHealthCheck("http", "v1/tests/alive")
        
        .WithExternalHttpEndpoints()
        .WithEnvironment("Features__EnableLoadShedding", enableLoadShedding.ToString())
        .WithEnvironment("Features__EnableRateLimiting", enableRateLimiting.ToString())

        // Emailer
        .WithEnvironment("EmailOptions__EmailTransport", "Smtp")
        .WithEnvironment("EmailOptions__DefaultEmailFrom", "noreply@test.com")
        .WithEnvironment("EmailOptions__DefaultEmailTo", "devteam@distinction.co.uk")
        .WithEnvironment("EmailOptions__Host", "localhost")
        
        // Authentication
        .WithEnvironment("Authentication__AuthenticationProvider",
            Enum.GetName(typeof(AuthenticationProviders), AuthenticationProviders.Keycloak))
        .WithEnvironment("Authentication__Audience", "https://TemplateProject.api")
        .WithEnvironment("Authentication__RequireHttpsMetadata", false.ToString())
        .WithEnvironment("Authentication__SwaggerClientId", "swagger-ui")
        .WithEnvironment("Authentication__ManagementClientId", "admin-cli")
        .WithEnvironment("Authentication__ManagementClientSecret", "admin:admin") // user:pass
        .WithEnvironment("Authentication__AuthServerUrl", $"{keycloak.Resource.GetEndpoint("http")}")
        .WithEnvironment("Authentication__Realm", "TemplateProject")
        .WithReference(keycloak)
        .WaitFor(keycloak)

        // Database
        .WithEnvironment("Database__DatabaseProvider",
            Enum.GetName(typeof(DatabaseProviders), dbOptions.DatabaseProvider))
        .WithEnvironment("Database__ConnectionStringName", dbOptions.ConnectionStringName)
        .WithReference(db, dbOptions.ConnectionStringName)
        .WaitFor(db)

        //AMQP
        .WithEnvironment("AMQP__Transport", Enum.GetName(typeof(AmqpTransports), amqpOptions.Transport))
        .WithEnvironment("AMQP__ConnectionStringName", amqpOptions.ConnectionStringName)
        .WithReference(amqp, amqpOptions.ConnectionStringName)
        .WaitFor(amqp)

        // Storage
        .WithEnvironment("Storage__StorageProvider",
            Enum.GetName(typeof(StorageProviders), storageOptions.StorageProvider))
        .WithEnvironment("Storage__ConnectionStringName", storageOptions.ConnectionStringName)
        .WithReference(storage, storageOptions.ConnectionStringName)

        // Redis cache
        .WithReference(redis, "Redis")
        .WaitFor(redis)
    ;

builder.AddProject<Projects.Application_Host_Worker>("TemplateProject-Worker")
    .WaitFor(api)
    
    // Database
    .WithEnvironment("Database__DatabaseProvider", Enum.GetName(typeof(DatabaseProviders), dbOptions.DatabaseProvider))
    .WithEnvironment("Database__ConnectionStringName", dbOptions.ConnectionStringName)
    .WithReference(db, dbOptions.ConnectionStringName)
    .WaitFor(db)

    // Emailer
    .WithEnvironment("EmailOptions__EmailTransport", "Smtp")
    .WithEnvironment("EmailOptions__DefaultEmailFrom", "noreply@test.com")
    .WithEnvironment("EmailOptions__DefaultEmailTo", "devteam@distinction.co.uk")
    .WithEnvironment("EmailOptions__Host", "localhost")
    
    // Authentication
    .WithEnvironment("Authentication__AuthenticationProvider",
        Enum.GetName(typeof(AuthenticationProviders), AuthenticationProviders.Keycloak))
    .WithEnvironment("Authentication__Audience", "https://TemplateProject.api")
    .WithEnvironment("Authentication__RequireHttpsMetadata", false.ToString())
    .WithEnvironment("Authentication__SwaggerClientId", "swagger-ui")
    .WithEnvironment("Authentication__ManagementClientId", "admin-cli")
    .WithEnvironment("Authentication__ManagementClientSecret", "admin:admin") // user:pass
    .WithEnvironment("Authentication__AuthServerUrl", $"{keycloak.Resource.GetEndpoint("http")}")
    .WithEnvironment("Authentication__Realm", "TemplateProject")
    .WithReference(keycloak)
    .WaitFor(keycloak)
    
    //AMQP
    .WithEnvironment("AMQP__Transport", Enum.GetName(typeof(AmqpTransports), amqpOptions.Transport))
    .WithEnvironment("AMQP__ConnectionStringName", amqpOptions.ConnectionStringName)
    .WithReference(amqp, amqpOptions.ConnectionStringName)
    .WaitFor(amqp)

    // Storage
    .WithEnvironment("Storage__StorageProvider", Enum.GetName(typeof(StorageProviders), storageOptions.StorageProvider))
    .WithEnvironment("Storage__ConnectionStringName", storageOptions.ConnectionStringName)
    .WithReference(storage, storageOptions.ConnectionStringName)

    // Redis cache
    .WithReference(redis, "Redis")
    .WaitFor(redis)
    ;

switch (presentationOptions.LaunchApp)
{
    case "Blazor":
        builder.AddProject<Projects.Presentation_Blazor>("TemplateProject-Blazor")
            // Authentication
            .WithEnvironment("Authentication__Authority", $"{keycloak.Resource.GetEndpoint("http")}/realms/TemplateProject")
            .WithEnvironment("Authentication__ClientId", "TemplateProject-web-client") // local-dev only not required in production
            .WithEnvironment("Authentication__Audience", "https://TemplateProject.api")
            .WithEnvironment("Authentication__RequireHttpsMetadata", false.ToString())
            .WaitFor(keycloak) // even though the API already waits for this it's good practice to be explicit
            
            .WithEnvironment("ApiBaseUrl", $"{api.Resource.GetEndpoint("https")}")
            .WaitFor(api);
        break;
    
    case "NextJS":
        // TODO : Add NPM run
        throw new NotImplementedException("Next JS is not yet implemented");
        break;
    
    default:
        // Do nothing since we still want everything else to start up even if we're running 
        // without a presentation layer
        break;
}

builder.Build().Run();