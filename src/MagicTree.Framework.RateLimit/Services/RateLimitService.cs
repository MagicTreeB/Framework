using MagicTree.Framework.RateLimit.Interfaces;
using MagicTree.Framework.RateLimit.Models;

namespace MagicTree.Framework.RateLimit.Services;

/// <summary>
/// Sliding window rate limit service implementation
/// </summary>
public class RateLimitService : IRateLimitService
{
    private readonly IRateLimitStorage _storage;

    public RateLimitService(IRateLimitStorage storage)
    {
        _storage = storage;
    }

    public async Task<RateLimitResult> CheckRateLimitAsync(
        string identifier,
        string endpoint,
        int limit,
        int windowSeconds,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(identifier, endpoint);
        var now = DateTimeOffset.UtcNow;
        var windowExpiration = TimeSpan.FromSeconds(windowSeconds);

        // Increment counter
        var currentCount = await _storage.IncrementAsync(key, windowExpiration, cancellationToken);

        // Get TTL for reset time
        var ttl = await _storage.GetTtlAsync(key, cancellationToken);
        var resetAt = ttl.HasValue 
            ? now.Add(ttl.Value).ToUnixTimeSeconds() 
            : now.AddSeconds(windowSeconds).ToUnixTimeSeconds();

        var retryAfterSeconds = ttl.HasValue ? (int)ttl.Value.TotalSeconds : windowSeconds;

        var result = new RateLimitResult
        {
            IsAllowed = currentCount <= limit,
            CurrentCount = (int)currentCount,
            Limit = limit,
            ResetAt = resetAt,
            RetryAfterSeconds = retryAfterSeconds,
            Identifier = identifier,
            Endpoint = endpoint
        };

        return result;
    }

    public async Task ResetAsync(string identifier, string endpoint, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(identifier, endpoint);
        await _storage.ResetAsync(key, cancellationToken);
    }

    private static string BuildKey(string identifier, string endpoint)
    {
        // Sanitize endpoint (remove query string, normalize slashes)
        var cleanEndpoint = endpoint.Split('?')[0].Trim('/').Replace('/', ':');
        return $"ratelimit:{identifier}:{cleanEndpoint}";
    }
}
