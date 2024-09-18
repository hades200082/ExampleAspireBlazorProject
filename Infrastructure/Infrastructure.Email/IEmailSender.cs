namespace Infrastructure.Email;

public interface IEmailSender
{
    Task SendEmailAsync(string to, string templateId, object data, bool throwOnFail = false, CancellationToken cancellationToken = default);
}