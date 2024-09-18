namespace Infrastructure.Email;

/// <summary>
/// Must be implemented in domain.services on a project-by-project basis
/// </summary>
public interface IEmailTemplateProvider
{
    Task<string> GetTemplateAsync(string templateId);
}