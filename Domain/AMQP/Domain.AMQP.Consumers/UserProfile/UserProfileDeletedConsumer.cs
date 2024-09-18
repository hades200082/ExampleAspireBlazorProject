using System.Diagnostics.CodeAnalysis;
using Domain.AMQP.MessageContracts.Events;
using MassTransit;

namespace Domain.AMQP.Consumers.UserProfile;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class UserProfileDeletedConsumer : IConsumer<EntityDeleted>
{
    public async Task Consume(ConsumeContext<EntityDeleted> context)
    {
        await Task.Yield(); // Remove this line once async is implemented below.
        
        if (context.Message.Entity is Entities.UserProfile)
        {
            // Do stuff here if needed. For example:
            //     - Send an email to the user that their profile has been deleted
            //     - Cleanup / optimise indexes, other records, etc.
        }
    }
}

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class UserProfileDeletedConsumerDefinition : ConsumerDefinition<UserProfileDeletedConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<UserProfileDeletedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(cfg =>
        {
            cfg.SetRetryPolicy(filter =>
                filter.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
        });
    }
}