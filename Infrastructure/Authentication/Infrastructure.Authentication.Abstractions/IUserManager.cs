namespace Infrastructure.Authentication.Abstractions;

public interface IUserManager
{
    Task<ExternalUser?> GetUser(string externalId);

    Task<bool> IsEmailVerified(string externalId);
    Task<bool> IsEnabled(string externalId);
    Task UpdatePassword(string externalId, string newPassword);
    Task UpdateEmail(string externalId, string newEmail, bool requireVerification = false);
    Task UpdatePhone(string externalId, string newPhone, bool requireVerification = false);
    Task UpdateUserName(string externalId, string newUserName);
    Task SendEmailVerify(string externalId);
    Task DisableUser(string externalId);
    Task EnableUser(string externalId);
    Task DeleteUser(string externalId);
    
    Task UpdateCustomData(string externalId, Dictionary<string, List<string>> data);
}