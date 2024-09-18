using Domain.Entities;
using Domain.Services;
using Infrastructure.Authentication.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Resilience;
using Microsoft.EntityFrameworkCore;

namespace Domain.Startup;

public static class Extensions
{
    public static async Task AwaitDatabaseReadiness(this IHost app, bool automaticMigrations = false,
        CancellationToken cancellationToken = default)
    {
        var scope = app.Services.CreateScope();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var permisionProvider = scope.ServiceProvider.GetRequiredService<IPermissionProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IHost>>();
        logger.LogInformation("Waiting for database to be responsive");

        // Check that we're using a relational database before trying to apply migrations
        if (!db.Database.IsRelational()) return;

        var migrationsPending = false;
        await PollyRetryPolicies.DatabaseStartupResiliencePipeline.ExecuteAsync(
                async ct =>
                {
                    migrationsPending =
                        (await db.Database.GetPendingMigrationsAsync(cancellationToken: ct)).Any();
                },
                cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("Database is now responding - handling migrations checks");

        if (automaticMigrations && migrationsPending)
        {
            logger.LogInformation("Automatically applying new migrations");
            
            try
            {
                await db.Database.MigrateAsync(cancellationToken: cancellationToken);
                logger.LogInformation("Migrations applied");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Migrations failed");
                throw new StartupException("Migrations failed", ex);
            }
        }
        else if (migrationsPending)
        {
            logger.LogInformation("There are pending migrations - waiting for them to be applied");
            await PollyRetryPolicies.RetryForeverWhenTruePipeline.ExecuteAsync(
                async ct => (await db.Database.GetPendingMigrationsAsync(cancellationToken: ct)).Any(),
                cancellationToken);
            logger.LogInformation("Migrations have been applied - continuing startup");
        }
        
        // Once we get here the database is ready
        await SeedData(db, env, permisionProvider, automaticMigrations, cancellationToken);
    }

    private static async Task SeedData(AppDbContext db, IHostEnvironment environment, IPermissionProvider permissionProvider, bool automaticMigrations = false,
        CancellationToken cancellationToken = default)
    {
        if (!automaticMigrations) return;
        
        // Seed the global admin role if it doesn't exist
        var needsSave = false;
        var permissions = await permissionProvider.GetAllPermissions(cancellationToken);
        
        // Get the GA role
        var ga = await db.Roles.FirstOrDefaultAsync(x => x.Name == Role.GlobalAdministrator, cancellationToken);
        if (ga is null)
        {
            ga = new Role
            {
                Id = Role.GlobalAdministratorId,
                Name = Role.GlobalAdministrator,
                Description = "Global Administrator",
                Permissions = permissions
            };
            await db.Roles.AddAsync(ga, cancellationToken);
            needsSave = true;
        }
        
        if (environment.IsDevelopment())
        {
            // Ensure we have our seed users in development
            const string testUser1Id = "4592defe-abdf-43bd-833d-4dede705b5aa"; // This ID is taken from the included Keycloak database - DO NOT CHANGE
            if (!await db.UserProfiles.AnyAsync(up => up.Id == testUser1Id, cancellationToken: cancellationToken))
            {
                db.UserProfiles.Add(new UserProfile
                {
                    Id = testUser1Id,
                    Name = "Administrator",
                    Email = "test-user-1@test.com",
                    Roles = [ ga ]
                });
                needsSave = true;
            }

            const string testUser2Id = "ef8ba546-2efc-4347-b81e-4a0210b9b7a3";
            if (!await db.UserProfiles.AnyAsync(up => up.Id == testUser2Id, cancellationToken: cancellationToken))
            {
                db.UserProfiles.Add(new UserProfile
                {
                    Id = testUser2Id,
                    Name = "User",
                    Email = "test-user-2@test.com",
                    Roles = [ ]
                });
                needsSave = true;
            }
        }
        
        if (needsSave) await db.SaveChangesAsync(cancellationToken);
    }
}