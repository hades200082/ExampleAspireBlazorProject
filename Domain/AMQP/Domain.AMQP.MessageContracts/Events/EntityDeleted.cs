namespace Domain.AMQP.MessageContracts.Events;

public interface EntityDeleted
{
    object Entity { get; set; }
}