using System.ComponentModel.DataAnnotations;
using Shared.Enums;

namespace AppHost.Options;

public sealed class DatabaseOptions
{
    [Required]
    [EnumDataType(typeof(DatabaseProviders))]
    public DatabaseProviders DatabaseProvider { get; init; }

    [Required] public string ConnectionStringName { get; init; } = null!;
}