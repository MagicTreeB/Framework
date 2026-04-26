using StackExchange.Redis;
using MMO.Core.RateLimit.Interfaces;

namespace MMO.Core.RateLimit.Storage;

/// <summary>
/// Redis-based rate limit storage for distributed systems
/// Suitable for multi-instance deployments
/// </summary>
public class RedisRateLimitStorage : IRateLimitStorage
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisRateLimitStorage(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = redis.GetDatabase();
    }

    public async Task<long> IncrementAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        var count = await _db.StringIncrementAsync(key);
        
        // Set expiration only on first increment
        if (count == 1)
        {
            await _db.KeyExpireAsync(key, expiration);
        }

        return count;
    }

    public async Task<long> GetCountAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? (long)value : 0L;
    }

    public async Task<TimeSpan?> GetTtlAsync(string key, CancellationToken cancellationToken = default)
    {
        var ttl = await _db.KeyTimeToLiveAsync(key);
        return ttl;
    }

    public async Task ResetAsync(string key, CancellationToken cancellationToken = default)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _db.KeyExistsAsync(key);
    }
}
