using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MMO.Core.RateLimit.Interfaces;
using MMO.Core.RateLimit.Options;
using System.Net;
using System.Text.Json;

namespace MMO.Core.RateLimit.Middlewares;

/// <summary>
/// Middleware for rate limiting HTTP requests
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitOptions _options;
    private readonly IRateLimitService _rateLimitService;

    public RateLimitMiddleware(
        RequestDelegate next,
        IOptions<RateLimitOptions> options,
        IRateLimitService rateLimitService)
    {
        _next = next;
        _options = options.Value;
        _rateLimitService = rateLimitService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip if rate limiting is disabled
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        // Get identifier (IP, User, or Client)
        var identifier = GetIdentifier(context);

        // Check whitelist
        if (_options.IpWhitelist.Contains(identifier))
        {
            await _next(context);
            return;
        }

        // Get endpoint-specific or global rule
        var endpoint = context.Request.Path.Value ?? "/";
        var rule = GetRuleForEndpoint(endpoint);

        // Check rate limit
        var result = await _rateLimitService.CheckRateLimitAsync(
            identifier,
            endpoint,
            rule.Limit,
            rule.WindowSeconds,
            context.RequestAborted);

        // Add rate limit headers
        if (_options.Headers.IncludeHeaders)
        {
            context.Response.Headers[_options.Headers.LimitHeader] = rule.Limit.ToString();
            context.Response.Headers[_options.Headers.RemainingHeader] = result.Remaining.ToString();
            context.Response.Headers[_options.Headers.ResetHeader] = result.ResetAt.ToString();
        }

        // Check if request is allowed
        if (!result.IsAllowed)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers[_options.Headers.RetryAfterHeader] = result.RetryAfterSeconds.ToString();
            context.Response.ContentType = "application/json";
            
            var json = JsonSerializer.Serialize(new
            {
                error = "Rate limit exceeded",
                message = $"Too many requests. Please try again in {result.RetryAfterSeconds} seconds.",
                limit = result.Limit,
                remaining = result.Remaining,
                resetAt = result.ResetAt
            });
            
            await context.Response.WriteAsync(json);
            
            return;
        }

        await _next(context);
    }

    private string GetIdentifier(HttpContext context)
    {
        // Priority: User ID > Client ID > IP Address
        var userId = context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        var clientId = context.Request.Headers["X-Client-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(clientId))
        {
            return $"client:{clientId}";
        }

        // Get real IP (handle proxies and load balancers)
        var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? context.Request.Headers["X-Real-IP"].FirstOrDefault()
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return $"ip:{ip}";
    }

    private RateLimitRule GetRuleForEndpoint(string endpoint)
    {
        // Check for exact match
        if (_options.Endpoints.TryGetValue(endpoint, out var rule))
        {
            return rule;
        }

        // Check for pattern match (e.g., /api/auth/*)
        foreach (var (pattern, endpointRule) in _options.Endpoints)
        {
            if (pattern.EndsWith("*") && endpoint.StartsWith(pattern.TrimEnd('*')))
            {
                return endpointRule;
            }
        }

        // Return global rule
        return _options.Global;
    }
}
