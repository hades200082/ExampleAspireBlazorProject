using Shared.Enums;

namespace Infrastructure.MassTransit;

public sealed class TransportOptions
{
    public required AmqpTransports Transport { get; set; }
    public required string ConnectionStringName { get; set; }
}