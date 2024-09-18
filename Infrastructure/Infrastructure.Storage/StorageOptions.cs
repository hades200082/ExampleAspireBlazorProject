using Shared.Enums;

namespace Infrastructure.Storage;

public sealed class StorageOptions
{
    public required StorageProviders StorageProvider { get; init; }
    public required string ConnectionStringName { get; init; }
}