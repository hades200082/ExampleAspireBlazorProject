using Shared.Enums;

namespace AppHost.Options;

public sealed class AmqpOptions
{
    public required AmqpTransports Transport { get; set; }
    public required string ConnectionStringName { get; set; }
}