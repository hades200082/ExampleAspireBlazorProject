using FluentEmail.Core;
using FluentEmail.Core.Models;
using FluentEmail.Mailgun;
using FluentEmail.SendGrid;
using Microsoft.Extensions.Logging;
using Shared.Enums;

namespace Infrastructure.Email;

public class EmailSender(
    IFluentEmail fluentEmail,
    IEmailTemplateProvider emailTemplateProvider,
    ILogger<EmailSender> logger,
    IEmailOptions emailOptions
) : IEmailSender
{
    public async Task SendEmailAsync(string to, string templateId, object data, bool throwOnFail = false, CancellationToken cancellationToken = default)
    {
        var msg = fluentEmail.To(to);

        var (remoteApiEmailOptions, remoteTemplateId) = IsRemoteAllowed(emailOptions, templateId);
        
        if (remoteApiEmailOptions is not null && !string.IsNullOrEmpty(remoteTemplateId))
        {
            SendResponse? response = null;
            switch (remoteApiEmailOptions.EmailTransport)
            {
                case EmailTransports.SendGrid:
                    response = await msg.SendWithTemplateAsync(templateId: remoteTemplateId, data);
                    break;
                case EmailTransports.MailGun:
                    response = await msg.SendWithTemplateAsync(templateName: remoteTemplateId, data);
                    break;
            }

            if (!response?.Successful ?? true)
            {
                logger.LogError("Error sending templated email: {Err}", response?.ErrorMessages.Aggregate("", (current, error) => current + error));

                if (throwOnFail)
                    throw new Exception("Error sending templated email");
            }
        }
        else
        {
            var template = await emailTemplateProvider.GetTemplateAsync(templateId);
            var response = await msg.UsingTemplate(template, data).SendAsync(cancellationToken);
            
            if (!response?.Successful ?? true)
            {
                logger.LogError("Error sending templated email: {Err}", response?.ErrorMessages.Aggregate("", (current, error) => current + error));
                
                if (throwOnFail)
                    throw new Exception("Error sending templated email");
            }
        }
    }
    
    private (RemoteApiEmailOptions?, string?) IsRemoteAllowed(IEmailOptions emailOptions, string templateId)
    {
        if (emailOptions is RemoteApiEmailOptions {UseRemoteTemplates: true} options)
        {
            if (options.TemplateIdMapping.TryGetValue(templateId, out var remoteTemplateId))
            {
                return (options, remoteTemplateId);
            }

            logger.LogWarning("Remote Template Mapping for '{TemplateID}' not found, but remote enabled", templateId);
            return (options, null);
        }
        
        return (null, null);
    }

}