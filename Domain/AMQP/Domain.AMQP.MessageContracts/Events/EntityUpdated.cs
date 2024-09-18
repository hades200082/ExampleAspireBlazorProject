namespace Domain.AMQP.MessageContracts.Events;

public interface EntityUpdated
{
    object Entity { get; set; }
}