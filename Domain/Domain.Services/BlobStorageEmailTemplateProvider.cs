using FluentStorage.Blobs;
using Infrastructure.Email;

namespace Domain.Services;

public sealed class BlobStorageEmailTemplateProvider(IBlobStorage blobStorage) : IEmailTemplateProvider
{
    public async Task<string> GetTemplateAsync(string templateId)
    {
        var template = await blobStorage.ReadTextAsync($"EmailTemplates/{templateId}.liquid");
        NullEmailTemplateException.ThrowIfNullOrEmpty(template, templateId);
        return template;
    }
}