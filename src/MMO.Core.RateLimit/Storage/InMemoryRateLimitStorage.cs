using Microsoft.Extensions.Caching.Memory;
using MMO.Core.RateLimit.Interfaces;

namespace MMO.Core.RateLimit.Storage;

/// <summary>
/// In-memory rate limit storage using IMemoryCache
/// Suitable for single-instance deployments
/// </summary>
public class InMemoryRateLimitStorage : IRateLimitStorage
{
    private readonly IMemoryCache _cache;
    private readonly object _lock = new();

    public InMemoryRateLimitStorage(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<long> IncrementAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var cacheKey = $"{key}:count";
            var expirationKey = $"{key}:expiration";

            if (!_cache.TryGetValue(cacheKey, out long count))
            {
                count = 0;
                var expirationTime = DateTimeOffset.UtcNow.Add(expiration);
                
                _cache.Set(cacheKey, count, expiration);
                _cache.Set(expirationKey, expirationTime, expiration);
            }

            count++;
            _cache.Set(cacheKey, count, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = _cache.Get<DateTimeOffset>(expirationKey)
            });

            return Task.FromResult(count);
        }
    }

    public Task<long> GetCountAsync(string key, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{key}:count";
        var count = _cache.TryGetValue(cacheKey, out long value) ? value : 0L;
        return Task.FromResult(count);
    }

    public Task<TimeSpan?> GetTtlAsync(string key, CancellationToken cancellationToken = default)
    {
        var expirationKey = $"{key}:expiration";
        
        if (_cache.TryGetValue(expirationKey, out DateTimeOffset expiration))
        {
            var ttl = expiration - DateTimeOffset.UtcNow;
            return Task.FromResult<TimeSpan?>(ttl > TimeSpan.Zero ? ttl : null);
        }

        return Task.FromResult<TimeSpan?>(null);
    }

    public Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{key}:count";
        var expirationKey = $"{key}:expiration";
        
        _cache.Remove(cacheKey);
        _cache.Remove(expirationKey);
        
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{key}:count";
        var exists = _cache.TryGetValue(cacheKey, out _);
        return Task.FromResult(exists);
    }
}
