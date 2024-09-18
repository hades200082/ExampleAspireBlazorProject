using Infrastructure.Email;

namespace Domain.Services;

/// <summary>
/// Provides email templates stored in the database.
///
/// When using database email templates you must:
/// - ensure that the templates are seeded into the database
/// - provide a means for editing the templates through the admin ui
/// </summary>
/// <param name="db"></param>
public sealed class DatabaseEmailTemplateProvider(
    AppDbContext db
) : IEmailTemplateProvider
{
    public async Task<string> GetTemplateAsync(string templateId)
    {
        var template = (await db.EmailTemplates.FindAsync(templateId))?.Template;
        NullEmailTemplateException.ThrowIfNullOrEmpty(template, templateId);
        return template;
    }
}
