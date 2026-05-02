namespace MagicTree.Framework.RateLimit.Options;

/// <summary>
/// Rate limiting configuration options
/// </summary>
public class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Enable rate limiting globally
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Storage type: InMemory or Redis
    /// </summary>
    public string StorageType { get; set; } = "InMemory";

    /// <summary>
    /// Redis connection string (required if StorageType is Redis)
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Global rate limit rules
    /// </summary>
    public RateLimitRule Global { get; set; } = new();

    /// <summary>
    /// Endpoint-specific rate limit rules
    /// </summary>
    public Dictionary<string, RateLimitRule> Endpoints { get; set; } = new();

    /// <summary>
    /// IP whitelist - these IPs bypass rate limiting
    /// </summary>
    public List<string> IpWhitelist { get; set; } = new();

    /// <summary>
    /// Response headers configuration
    /// </summary>
    public RateLimitHeaders Headers { get; set; } = new();
}

/// <summary>
/// Rate limit rule configuration
/// </summary>
public class RateLimitRule
{
    /// <summary>
    /// Max requests allowed in the time window
    /// </summary>
    public int Limit { get; set; } = 100;

    /// <summary>
    /// Time window in seconds
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Rate limiting strategy: FixedWindow, SlidingWindow, TokenBucket
    /// </summary>
    public string Strategy { get; set; } = "SlidingWindow";

    /// <summary>
    /// Identifier type: IP, User, Client
    /// </summary>
    public string IdentifierType { get; set; } = "IP";
}

/// <summary>
/// Rate limit response headers configuration
/// </summary>
public class RateLimitHeaders
{
    /// <summary>
    /// Include X-RateLimit-* headers in responses
    /// </summary>
    public bool IncludeHeaders { get; set; } = true;

    /// <summary>
    /// Header name for limit
    /// </summary>
    public string LimitHeader { get; set; } = "X-RateLimit-Limit";

    /// <summary>
    /// Header name for remaining requests
    /// </summary>
    public string RemainingHeader { get; set; } = "X-RateLimit-Remaining";

    /// <summary>
    /// Header name for reset time
    /// </summary>
    public string ResetHeader { get; set; } = "X-RateLimit-Reset";

    /// <summary>
    /// Header name for retry after (when limited)
    /// </summary>
    public string RetryAfterHeader { get; set; } = "Retry-After";
}
