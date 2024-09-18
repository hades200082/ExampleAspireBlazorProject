using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Shared.Enums;

namespace Infrastructure.EntityFrameworkCore;

public static class Extensions
{
    public static IHostApplicationBuilder AddConfiguredDbContext<T>(this IHostApplicationBuilder builder)
        where T : DbContext
    {
        var options = builder.Configuration.GetSection("Database").Get<DatabaseOptions>();
        
        ArgumentNullException.ThrowIfNull(options?.DatabaseProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ConnectionStringName);
        
        var connectionString = builder.Configuration.GetConnectionString(options.ConnectionStringName);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        builder.Services.AddSingleton(options);

        switch (options.DatabaseProvider)
        {
            case DatabaseProviders.MySql:
            case DatabaseProviders.MariaDB:
                builder.Services.AddDbContextPool<T>(opt => opt
                    .UseMySql(
                        connectionString,
                        ServerVersion.AutoDetect(connectionString),
                        cfg => cfg.MigrationsAssembly("Domain.Migrations.MySql"))
                );
                builder.EnrichMySqlDbContext<T>();
                break;
            
            case DatabaseProviders.Postgres:
                builder.Services.AddDbContextPool<T>(opt => opt
                    .UseNpgsql(
                        connectionString,
                        cfg => cfg.MigrationsAssembly("Domain.Migrations.Postgres"))
                );
                builder.EnrichNpgsqlDbContext<T>();
                break;
            case DatabaseProviders.SqlServer:
                builder.Services.AddDbContextPool<T>(opt => opt
                    .UseSqlServer(
                        connectionString,
                        cfg => cfg.MigrationsAssembly("Domain.Migrations.SqlServer"))
                );
                builder.EnrichSqlServerDbContext<T>();
                break;
            case DatabaseProviders.CosmosDB:
                builder.Services.AddDbContextPool<T>(opt => opt
                    .UseCosmos(connectionString, options.ConnectionStringName)
                );
                builder.EnrichCosmosDbContext<T>();
                break;
            case DatabaseProviders.MongoDB:
                // TODO : Update EF Core only once Aspire and other MS libs have sorted out wht version of MongoDB.Driver to use. 
                builder.AddMongoDBClient(options.ConnectionStringName);
                builder.Services.AddDbContextPool<T>((services, cfg) =>
                {
                    var client = services.GetRequiredService<IMongoClient>();
                    cfg.UseMongoDB(client, options.ConnectionStringName);
                });
                break;
        }

        return builder;
    }
}