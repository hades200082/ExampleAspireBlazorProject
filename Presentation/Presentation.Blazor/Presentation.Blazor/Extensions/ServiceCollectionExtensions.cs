using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace Presentation.Blazor.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServerApiClient<TInterface, TImplementation>(this IServiceCollection services, string apiBaseUrl)
        where TImplementation : class, TInterface
        where TInterface : class
    {
        services.AddHttpClient<TInterface, TImplementation>(async (sp, client) =>
        {
            var ctx = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
            var token = ctx is not null 
                ? await ctx.GetTokenAsync("access_token")
                : null;
            
            if(!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
        }).AddStandardResilienceHandler();
        
        return services;
    }
}