using System.Security.Claims;
using System.Text.Json;
using Application.Host.Api.Extensions;
using Microsoft.Extensions.Caching.Distributed;

namespace Application.Host.Api.Middleware;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class ApiOutputCacheMiddleware(
    RequestDelegate next,
    IDistributedCache cache,
    ILogger<ApiOutputCacheMiddleware> logger
)
{
    private const string CacheKeyListPrefix = "CacheKeys:";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.RouteValues["controller"] is not null)
        {
            if (context.Request.Method == HttpMethods.Get)
            {
                await HandleGetRequestAsync(context);
            }
            else if (HttpMethods.IsPost(context.Request.Method) ||
                     HttpMethods.IsPut(context.Request.Method) ||
                     HttpMethods.IsPatch(context.Request.Method) ||
                     HttpMethods.IsDelete(context.Request.Method))
            {
                await HandleMutatingRequestAsync(context);
            }
            else
            {
                await next(context);
            }
        }
        else
        {
            await next(context);
        }
    }

    private async Task HandleGetRequestAsync(HttpContext context)
    {
        var cacheKey = GenerateCacheKey(context);
        var cachedResponse = await cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedResponse))
        {
            logger.LogDebug("[ApiOutputCacheMiddleware] Returning cached response for '{key}'", cacheKey);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(cachedResponse);
        }
        else
        {
            var originalBodyStream = context.Response.Body;

            using var memoryStream = new MemoryStream();

            context.Response.Body = memoryStream;

            await next(context);

            context.Response.Body = originalBodyStream;

            if (context.Response.StatusCode == StatusCodes.Status200OK)
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
                memoryStream.Seek(0, SeekOrigin.Begin);

                logger.LogDebug("[ApiOutputCacheMiddleware] Caching new response for '{key}'", cacheKey);
                
                var cacheOptions = new DistributedCacheEntryOptions();
                cacheOptions.SetSlidingExpiration(TimeSpan.FromMinutes(1));
                cacheOptions.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                
                await cache.SetStringAsync(cacheKey, responseBody, cacheOptions);
                await AddCacheKeyToList(context, cacheKey);

                await memoryStream.CopyToAsync(originalBodyStream);
            }
        }
    }

    private async Task HandleMutatingRequestAsync(HttpContext context)
    {
        await next(context);

        if (context.Response.StatusCode is >= StatusCodes.Status200OK and < StatusCodes.Status300MultipleChoices)
        {
            var userId = context.User.GetIdentity() ?? "anonymous";
            var controllerName = context.Request.RouteValues["controller"]?.ToString();

            if (!string.IsNullOrEmpty(controllerName))
            {
                var cacheKeyListKey = $"{CacheKeyListPrefix}{userId}:{controllerName}";
                var cacheKeyList = await cache.GetStringAsync(cacheKeyListKey);

                if (!string.IsNullOrEmpty(cacheKeyList))
                {
                    var keys = JsonSerializer.Deserialize<List<string>>(cacheKeyList) ?? [];
                    foreach (var key in keys)
                    {
                        logger.LogDebug("[ApiOutputCacheMiddleware] Clearing cached response for '{key}'", key);
                        await cache.RemoveAsync(key);
                    }

                    await cache.RemoveAsync(cacheKeyListKey);
                }
            }
        }
    }

    private async Task AddCacheKeyToList(HttpContext context, string cacheKey)
    {
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var controllerName = context.Request.RouteValues["controller"]?.ToString();
        var cacheKeyListKey = $"{CacheKeyListPrefix}{userId}:{controllerName}";

        var cacheKeyList = await cache.GetStringAsync(cacheKeyListKey) ?? "[]";
        var keys = JsonSerializer.Deserialize<List<string>>(cacheKeyList) ?? [];
        keys.Add(cacheKey);

        await cache.SetStringAsync(cacheKeyListKey, JsonSerializer.Serialize(keys));
    }

    private string GenerateCacheKey(HttpContext context)
    {
        var userId = context.User.GetIdentity() ?? "anonymous";
        var controllerName = context.Request.RouteValues["controller"]?.ToString();
        var actionPath = context.Request.Path;
        var normalizedQueryString = NormalizeQueryString(context.Request.Query);

        return $"{userId}:{controllerName}:{actionPath}:{normalizedQueryString}";
    }

    private string NormalizeQueryString(IQueryCollection query)
    {
        var sortedQueryParameters = query.OrderBy(kv => kv.Key)
            .ToDictionary(kv => kv.Key, kv => string.Join(",", kv.Value.ToArray()));

        return JsonSerializer.Serialize(sortedQueryParameters);
    }
}

// Extension method to add the middleware
public static class CustomCacheMiddlewareExtensions
{
    public static IApplicationBuilder UseApiOutputCache(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiOutputCacheMiddleware>();
    }
}