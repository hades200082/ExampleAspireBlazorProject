namespace Domain.AMQP.MessageContracts.Events;

public interface EntityCreated
{
    object Entity { get; set; }
}