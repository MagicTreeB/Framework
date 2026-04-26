namespace MMO.Core.RateLimit.Interfaces;

/// <summary>
/// Storage interface for rate limit counters
/// </summary>
public interface IRateLimitStorage
{
    /// <summary>
    /// Increment counter and return current count
    /// </summary>
    Task<long> IncrementAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current counter value
    /// </summary>
    Task<long> GetCountAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get remaining TTL for key
    /// </summary>
    Task<TimeSpan?> GetTtlAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset counter for specific key
    /// </summary>
    Task ResetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if key exists
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
