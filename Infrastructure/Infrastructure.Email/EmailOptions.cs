using System.Net.Mail;
using FluentEmail.Mailgun;
using Shared.Enums;

namespace Infrastructure.Email;

public interface IEmailOptions
{
    EmailTransports EmailTransport { get; set; }
    string DefaultEmailFrom { get; set; }
    string DefaultEmailTo { get; set; }
}

public abstract class EmailOptions : IEmailOptions
{
    public EmailTransports EmailTransport { get; set; }
    public string DefaultEmailFrom { get; set; }
    public string DefaultEmailTo { get; set; }
}

public class RemoteApiEmailOptions : EmailOptions
{
    public string ApiKey { get; set; }
    public bool UseRemoteTemplates { get; set; }
    public Dictionary<string, string> TemplateIdMapping { get; set; }
}

public sealed class SmtpEmailOptions : EmailOptions
{
    public string Host { get; set; }
    public int Port { get; set; } = 25;
    public string Username { get; set; }
    public string Password { get; set; }
    public bool UseTls { get; set; }
}

public sealed class MailgunEmailOptions : RemoteApiEmailOptions
{
    public MailGunRegion Region { get; set; } = MailGunRegion.EU;
    public string Domain { get; set; }
}