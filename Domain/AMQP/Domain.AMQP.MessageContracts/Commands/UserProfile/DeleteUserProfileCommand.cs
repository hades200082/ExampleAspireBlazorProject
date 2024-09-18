namespace Domain.AMQP.MessageContracts.Commands.UserProfile;

public interface DeleteUserProfileCommand
{
    public string UserId { get; set; }
}