using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Enums;

namespace Infrastructure.Email;

public static class Extensions
{
    public static IEmailOptions? GetEmailOptions(this IHostApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection("EmailOptions");
        if (!section.Exists()) return null;
        
        var transport = section.GetValue<EmailTransports>("EmailTransport");

        return transport switch
        {
            EmailTransports.Smtp => section.Get<SmtpEmailOptions>(),
            EmailTransports.SendGrid or EmailTransports.MailGun => section.Get<RemoteApiEmailOptions>(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static IHostApplicationBuilder AddEmailSender(this IHostApplicationBuilder builder)
    {
        var options = builder.GetEmailOptions();
        if (options is null) return builder;
        
        builder.Services.AddSingleton<IEmailOptions>(options);

        var fluentEmailBuilder = builder.Services
            .AddFluentEmail(options.DefaultEmailFrom)
            .AddLiquidRenderer();

        if (options is SmtpEmailOptions smtpOptions)
        {
            var smtpClient = new SmtpClient(smtpOptions.Host, smtpOptions.Port)
            {
                EnableSsl = smtpOptions.UseTls,
                Credentials = new NetworkCredential(smtpOptions.Username, smtpOptions.Password)
            };
            fluentEmailBuilder.AddSmtpSender(smtpClient);
        }
        else if (options is RemoteApiEmailOptions remoteOptions)
        {

            switch (remoteOptions.EmailTransport)
            {
                case EmailTransports.SendGrid:
                    var useSandboxIfAvailable = false;
#if DEBUG
                    useSandboxIfAvailable = true;
#endif
                    fluentEmailBuilder.AddSendGridSender(remoteOptions.ApiKey, useSandboxIfAvailable);
                    break;

                case EmailTransports.MailGun when remoteOptions is MailgunEmailOptions mailgunOptions:
                    fluentEmailBuilder.AddMailGunSender(mailgunOptions.Domain, mailgunOptions.ApiKey,
                        mailgunOptions.Region);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        builder.Services.AddSingleton<IEmailSender, EmailSender>();
        
        return builder;
    }
}