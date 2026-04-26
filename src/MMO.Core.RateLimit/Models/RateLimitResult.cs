namespace MMO.Core.RateLimit.Models;

/// <summary>
/// Rate limit check result
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// Whether the request is allowed
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Current request count in the window
    /// </summary>
    public int CurrentCount { get; set; }

    /// <summary>
    /// Maximum allowed requests
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Remaining requests in current window
    /// </summary>
    public int Remaining => Math.Max(0, Limit - CurrentCount);

    /// <summary>
    /// When the rate limit resets (Unix timestamp)
    /// </summary>
    public long ResetAt { get; set; }

    /// <summary>
    /// Seconds until reset
    /// </summary>
    public int RetryAfterSeconds { get; set; }

    /// <summary>
    /// Identifier used for rate limiting (IP, user ID, etc.)
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint that was rate limited
    /// </summary>
    public string? Endpoint { get; set; }
}
