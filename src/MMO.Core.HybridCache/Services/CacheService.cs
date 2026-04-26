using Microsoft.Extensions.Caching.Hybrid;

namespace MMO.Core.HybridCache.Services;

/// <summary>
/// Service for HybridCache operations with common patterns
/// </summary>
public class CacheService(
    Microsoft.Extensions.Caching.Hybrid.HybridCache cache,
    Options.HybridCacheConfig options)
{
    private readonly Microsoft.Extensions.Caching.Hybrid.HybridCache _cache = cache;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(options.DefaultExpirationMinutes);
    private readonly TimeSpan _defaultLocalExpiration = TimeSpan.FromMinutes(options.LocalCacheExpirationMinutes);

    /// <summary>
    /// Get or create cache entry with default expiration
    /// </summary>
    public async Task<TResult> GetOrCreateAsync<TResult>(
        string key,
        Func<CancellationToken, Task<TResult>> factory,
        CancellationToken cancellationToken = default)
    {
        return await GetOrCreateAsync(key, factory, null, null, cancellationToken);
    }

    /// <summary>
    /// Get or create cache entry with custom expiration
    /// </summary>
    public async Task<TResult> GetOrCreateAsync<TResult>(
        string key,
        Func<CancellationToken, Task<TResult>> factory,
        TimeSpan? expiration,
        TimeSpan? localExpiration = null,
        CancellationToken cancellationToken = default)
    {
        var options = new HybridCacheEntryOptions
        {
            Expiration = expiration ?? _defaultExpiration,
            LocalCacheExpiration = localExpiration ?? _defaultLocalExpiration
        };

        return await _cache.GetOrCreateAsync(
            key, 
            cancellationToken => new ValueTask<TResult>(factory(cancellationToken)),
            options,
            tags: null,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Get or create cache entry with tags for invalidation
    /// </summary>
    public async Task<TResult> GetOrCreateAsync<TResult>(
        string key,
        Func<CancellationToken, Task<TResult>> factory,
        string[] tags,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        CancellationToken cancellationToken = default)
    {
        var options = new HybridCacheEntryOptions
        {
            Expiration = expiration ?? _defaultExpiration,
            LocalCacheExpiration = localExpiration ?? _defaultLocalExpiration
        };

        return await _cache.GetOrCreateAsync(
            key,
            cancellationToken => new ValueTask<TResult>(factory(cancellationToken)),
            options,
            tags,
            cancellationToken);
    }

    /// <summary>
    /// Set cache value directly
    /// </summary>
    public async Task SetAsync<TValue>(
        string key,
        TValue value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var options = new HybridCacheEntryOptions
        {
            Expiration = expiration ?? _defaultExpiration,
            LocalCacheExpiration = _defaultLocalExpiration
        };

        await _cache.SetAsync(key, value, options, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Remove cache entry by key
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }

    /// <summary>
    /// Remove cache entries by tag
    /// </summary>
    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveByTagAsync(tag, cancellationToken);
    }
}
