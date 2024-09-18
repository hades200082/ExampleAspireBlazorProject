namespace Presentation.Blazor.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiClient<TInterface, TImplementation>(this IServiceCollection services, string apiBaseUrl)
        where TImplementation : class, TInterface
        where TInterface : class
    {
        services.AddHttpClient<TInterface, TImplementation>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
        }).AddStandardResilienceHandler();
        
        return services;
    }
}