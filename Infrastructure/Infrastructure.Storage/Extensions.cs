using FluentStorage;
using FluentStorage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Enums;

namespace Infrastructure.Storage;

public static class Extensions
{
    public static IHostApplicationBuilder AddStorage(
        this IHostApplicationBuilder builder
    )
    {
        var options = builder.Configuration.GetSection("Storage").Get<StorageOptions>();
        
        ArgumentNullException.ThrowIfNull(options?.StorageProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ConnectionStringName);
        
        var connectionString = builder.Configuration.GetConnectionString(options.ConnectionStringName);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        builder.Services.AddSingleton(options);
        
        builder.Services.AddSingleton<IBlobStorage>(serviceProvider => {
            switch (options.StorageProvider)
            {
                case StorageProviders.LocalDisk:
                    if (!Path.IsPathFullyQualified(connectionString))
                    {
                        connectionString = $"/{connectionString.TrimStart('~').TrimStart('/').TrimStart('\\').Replace('\\', '/')}";
                    }
                    var path = Path.Join(builder.Environment.ContentRootPath, "wwwroot", connectionString);
                    return StorageFactory.Blobs.DirectoryFiles(path);

                case StorageProviders.SFTP:
                    StorageFactory.Modules.UseSftpStorage();
                    break;

                case StorageProviders.AzureBlobStorage:
                    StorageFactory.Modules.UseAzureBlobStorage();
                    break;

                case StorageProviders.AmazonS3:
                case StorageProviders.S3Compatible:
                    throw new NotImplementedException("S3 is not available");
                    // StorageFactory.Modules.UseAwsStorage();
                    // break;

                case StorageProviders.GoogleCloudStorage:
                    throw new NotImplementedException("GCS is not available");
                    // StorageFactory.Modules.UseGoogleCloudStorage();
                    // break;

                default:
                    throw new ArgumentException("Invalid storage service specified.");
            }
            
            return StorageFactory.Blobs.FromConnectionString(connectionString);
        });

        return builder;
    }
}