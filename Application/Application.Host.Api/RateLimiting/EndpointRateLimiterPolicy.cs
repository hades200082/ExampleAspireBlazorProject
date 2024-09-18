using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Application.Host.Api.Extensions;
using Microsoft.AspNetCore.RateLimiting;
using RedisRateLimiting;
using StackExchange.Redis;

namespace Application.Host.Api.RateLimiting;

internal sealed class EndpointRateLimiterPolicy : IRateLimiterPolicy<string>
{
    private readonly int _tokenLimit;
    private readonly int _tokensPerPeriod;
    private readonly TimeSpan _replenishmentPeriod;
    private readonly Func<OnRejectedContext, CancellationToken, ValueTask>? _onRejected;
    private readonly ConnectionMultiplexer? _redis;

    public EndpointRateLimiterPolicy(
        int tokenLimit,
        int tokensPerPeriod,
        TimeSpan replenishmentPeriod,
        ConnectionMultiplexer? redis = null)
    {
        _tokenLimit = tokenLimit;
        _tokensPerPeriod = tokensPerPeriod;
        _replenishmentPeriod = replenishmentPeriod;
        _redis = redis;
        _onRejected = (ctx, token) =>
        {
            ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return ValueTask.CompletedTask;
        };
    }

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var endpoint = httpContext.Request.Path.ToString();
        var user =  httpContext.User.GetIdentity() 
                    ?? (httpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback).ToString();

        var key = $"EndpointRateLimiter-{user}-{endpoint}";

        if (_redis is not null)
        {
            return RedisRateLimitPartition.GetTokenBucketRateLimiter(key, _ => new RedisTokenBucketRateLimiterOptions
            {
                ConnectionMultiplexerFactory = () => _redis,
                TokenLimit = _tokenLimit,
                TokensPerPeriod = _tokensPerPeriod,
                ReplenishmentPeriod = _replenishmentPeriod
            });
        }

        return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = _tokenLimit,
            TokensPerPeriod = _tokensPerPeriod,
            ReplenishmentPeriod = _replenishmentPeriod,
            AutoReplenishment = true,
            QueueLimit = 10,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });

    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => _onRejected;
}