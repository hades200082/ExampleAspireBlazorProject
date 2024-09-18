using AppHost.Options;
using AppHost.WaitforDependencies;
using Shared.Enums;

namespace AppHost.Extensions;

public static class DistributedApplicationBuilderExtensions
{
    private static Dictionary<DatabaseProviders, IResourceBuilder<IResourceWithConnectionString>> _databases =
        new();

    /// <summary>
    /// Gets a specific type of database **server**. If the requested type has been initialised before,
    /// returns a reference to the previous server resource.
    /// </summary>
    /// <remarks>Does not return a Database resource, only a DatabaseServer resource.</remarks>
    /// <param name="builder"></param>
    /// <param name="databaseType">The type of database server required</param>
    /// <param name="name">The name used for the connection string, server name and database name</param>
    /// <param name="password">A <see cref="IResourceBuilder{ParameterResource}"/> containing the database password</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static IResourceBuilder<IResourceWithConnectionString> GetDatabaseResource(
        this IDistributedApplicationBuilder builder,
        DatabaseProviders databaseType,
        string name,
        IResourceBuilder<ParameterResource> password
    )
    {
        return _databases.TryGetValue(databaseType, out var db)
            ? db
            : databaseType switch
            {
                DatabaseProviders.MySql or DatabaseProviders.MariaDB =>
                    builder.AddMySql(name, password: password)
                        .WithHealthCheck()
                        .WithDataVolume($"AppHost-{Enum.GetName(databaseType)}-{name}-data")
                        .WithPhpMyAdmin(),
                
                DatabaseProviders.Postgres =>
                    builder.AddPostgres(name, password: password)
                        .WithHealthCheck()
                        .WithDataVolume($"AppHost-{Enum.GetName(databaseType)}-{name}-data")
                        .WithPgAdmin(),
                
                DatabaseProviders.SqlServer =>
                    builder.AddSqlServer(name, password: password)
                        .WithHealthCheck()
                        .WithDataVolume($"AppHost-{Enum.GetName(databaseType)}-{name}-data")
                        .WithExternalHttpEndpoints(),
                
                /* TODO: Need to figure out if we can combine relational and nosql like this.
                    Ideally the way forward here would be to have a single abstract entity class
                    that has all the pieces needed for both and for it to just work.
                    It might need some if/else statements both in the type configs, and also in how
                    we write queries - which may mean a custom repository pattern (ugh!)
                 */
                DatabaseProviders.CosmosDB =>
                    builder.AddAzureCosmosDB(name) // No password param needed here
                        .WithHealthCheck(name)
                        .RunAsEmulator(b => b.WithExternalHttpEndpoints()), // Allow HTTP access to data explorer
                
                DatabaseProviders.MongoDB =>
                    builder.AddMongoDB(name) // No password param needed here
                        .WithHealthCheck()
                        .WithDataVolume($"AppHost-{Enum.GetName(databaseType)}-{name}-data")
                        .WithMongoExpress(),
                _ => throw new ArgumentOutOfRangeException(nameof(databaseType))
            };
    }

    /// <summary>
    /// Configures the chosen database
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="databaseType">The type of database server required</param>
    /// <param name="name">The name used for the connection string, server name and database name</param>
    /// <param name="password">A <see cref="IResourceBuilder{ParameterResource}"/> containing the database password</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static IResourceBuilder<IResourceWithConnectionString> AddDatabase(
        this IDistributedApplicationBuilder builder,
        DatabaseProviders databaseType,
        string name,
        IResourceBuilder<ParameterResource> password
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(databaseType);

        var serverName = $"{name}Server";
        var databaseName = $"{name}";
        var db = builder.GetDatabaseResource(databaseType, serverName, password);

        return db switch
        {
            IResourceBuilder<MySqlServerResource> mysql => mysql.AddDatabase(databaseName),
            IResourceBuilder<PostgresServerResource> postgres => postgres.AddDatabase(databaseName),
            IResourceBuilder<SqlServerServerResource> sqlserver => sqlserver.AddDatabase(databaseName),
            IResourceBuilder<AzureCosmosDBResource> cosmos => cosmos.AddDatabase(databaseName),
            IResourceBuilder<MongoDBServerResource> mongo => mongo.AddDatabase(databaseName),
            _ => throw new ArgumentException("No suitable database provider found")
        };
    }

    /// <summary>
    /// Configures the chosen AMQP transport
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="options"></param>
    /// <param name="databasePassword">Only used if Transport is SQL</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static IResourceBuilder<IResourceWithConnectionString> AddAmqp(
        this IDistributedApplicationBuilder builder,
        AmqpOptions options,
        IResourceBuilder<ParameterResource> databasePassword
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ConnectionStringName);
        ArgumentNullException.ThrowIfNull(options.Transport);

        return options.Transport switch
        {
            AmqpTransports.RabbitMQ =>
                builder
                    .AddRabbitMQ(options.ConnectionStringName, password: builder.AddParameter("AmqpPassword", true))
                    .WithHealthCheck()
                    .WithManagementPlugin()
                    .WithImage("masstransit/rabbitmq") // must come after `.WithManagementPlugin()`
                    .WithDataVolume(),
            
            AmqpTransports.AzureServiceBus =>
                builder.ExecutionContext.IsPublishMode
                    ? builder.AddAzureServiceBus(options.ConnectionStringName)
                    : builder.AddConnectionString(options.ConnectionStringName),
            
            AmqpTransports.AmazonSqs =>
                throw new NotImplementedException("AmazonSQS is not implemented"),
            
            AmqpTransports.SqlServer =>
                ((IResourceBuilder<SqlServerServerResource>) builder.GetDatabaseResource(
                    DatabaseProviders.SqlServer, "AMQPTransport-Server", databasePassword))
                .AddDatabase("AMQPTransport"),
            
            AmqpTransports.Postgres =>
                ((IResourceBuilder<PostgresServerResource>) builder.GetDatabaseResource(
                    DatabaseProviders.SqlServer, "AMQPTransport-Server", databasePassword))
                .AddDatabase("AMQPTransport"),
            
            _ => throw new ArgumentException("No suitable transport found")
        };
    }

    public static IResourceBuilder<IResourceWithConnectionString> AddStorageProvider(
        this IDistributedApplicationBuilder builder,
        StorageOptions options
    )
    {
        return options.StorageProvider switch
        {
            // TODO : Document
            StorageProviders.LocalDisk => builder.AddConnectionString("Storage_LocalDiskPath",
                options.ConnectionStringName),

            // TODO : Document
            StorageProviders.SFTP => builder.AddConnectionString("Storage_SFTP", options.ConnectionStringName),

            // Spins up Azurite locally
            StorageProviders.AzureBlobStorage => builder.AddAzureStorage(options.ConnectionStringName)
                .RunAsEmulator(ct => { ct.WithDataVolume(); }).AddBlobs("blob"),

            StorageProviders.AmazonS3 => throw new NotImplementedException("AmazonS3 is not yet supported"),

            StorageProviders.S3Compatible => throw new NotImplementedException("S3 compatible is not yet supported"),

            StorageProviders.GoogleCloudStorage => throw new NotImplementedException("GCS is not yet supported"),

            _ => throw new ArgumentException("No suitable storage provider found")
        };
    }
}