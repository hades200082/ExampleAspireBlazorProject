using System.Diagnostics.CodeAnalysis;
using Domain.AMQP.MessageContracts.Events;
using Infrastructure.Authentication.Abstractions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Domain.AMQP.Consumers.UserProfile;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class UserProfileEmailUpdatedConsumer(
    IUserManager userManager,
    ILogger<UserProfileEmailUpdatedConsumer> logger
) : IConsumer<EntityDeleted>
{
    public async Task Consume(ConsumeContext<EntityDeleted> context)
    {
        if (context.Message.Entity is Entities.UserProfile userProfile)
        {
            var oidcUser = await userManager.GetUser(userProfile.Id);
            if (oidcUser is not null && !oidcUser.Email.Equals(userProfile.Email, StringComparison.InvariantCultureIgnoreCase))
            {
                await userManager.UpdateEmail(
                    externalId: userProfile.Id,
                    newEmail: userProfile.Email,
                    requireVerification: true
                );
            }
        }
    }
}

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class UserProfileEmailUpdatedConsumerDefinition : ConsumerDefinition<UserProfileEmailUpdatedConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<UserProfileEmailUpdatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(cfg =>
        {
            cfg.SetRetryPolicy(filter =>
                filter.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
        });
    }
}