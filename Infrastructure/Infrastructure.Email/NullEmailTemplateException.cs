using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Email;

public sealed class NullEmailTemplateException : Exception
{
    public NullEmailTemplateException(string message) : base(message)
    {
    }
    
    public static void ThrowIfNullOrEmpty([NotNull] string? template, string templateId)
    {
        if (string.IsNullOrEmpty(template))
            throw new NullEmailTemplateException($"The template referenced by {templateId} is null or empty.");
    }
}