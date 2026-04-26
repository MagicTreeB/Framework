using MMO.Core.RateLimit.Models;

namespace MMO.Core.RateLimit.Interfaces;

/// <summary>
/// Interface for rate limiting service
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Check if request is allowed based on rate limit rules
    /// </summary>
    /// <param name="identifier">Unique identifier (IP, user ID, client ID)</param>
    /// <param name="endpoint">Endpoint path</param>
    /// <param name="limit">Max requests allowed</param>
    /// <param name="windowSeconds">Time window in seconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rate limit check result</returns>
    Task<RateLimitResult> CheckRateLimitAsync(
        string identifier,
        string endpoint,
        int limit,
        int windowSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset rate limit for specific identifier
    /// </summary>
    Task ResetAsync(string identifier, string endpoint, CancellationToken cancellationToken = default);
}
