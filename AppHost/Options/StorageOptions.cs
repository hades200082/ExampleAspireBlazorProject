using System.ComponentModel.DataAnnotations;
using Shared.Enums;

namespace AppHost.Options;

public sealed class StorageOptions
{
    [Required]
    [EnumDataType(typeof(DatabaseProviders))]
    public StorageProviders StorageProvider { get; init; }

    [Required] public string ConnectionStringName { get; init; } = null!;
}