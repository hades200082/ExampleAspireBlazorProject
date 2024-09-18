using Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Domain.Migrations.MySql;

/// <summary>
/// Factory class for creating an instance of the AppDbContext for design-time migrations.
/// </summary>
public class MigrationFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseMySql(new MySqlServerVersion(new Version(8,0)), opt =>
        {
            opt.MigrationsAssembly(typeof(MigrationFactory).Assembly.GetName().FullName);
        });
        return new AppDbContext(builder.Options);
    }
}