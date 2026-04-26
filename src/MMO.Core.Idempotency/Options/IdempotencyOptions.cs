namespace MMO.Core.Idempotency.Options;

/// <summary>
/// Configuration options for idempotency middleware
/// </summary>
public class IdempotencyOptions
{
    public static string SectionName => "Idempotency";

    /// <summary>
    /// Enable or disable idempotency middleware
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Storage type: "InMemory" or "Redis"
    /// </summary>
    public string StorageType { get; set; } = "InMemory";

    /// <summary>
    /// Redis connection string (required if StorageType is "Redis")
    /// </summary>
    public string RedisConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// How long to store idempotency records (in hours)
    /// </summary>
    public int ExpirationHours { get; set; } = 24;

    /// <summary>
    /// HTTP methods to apply idempotency to (default: POST, PUT, PATCH, DELETE)
    /// </summary>
    public List<string> HttpMethods { get; set; } = new() { "POST", "PUT", "PATCH", "DELETE" };

    /// <summary>
    /// Specific endpoints to apply idempotency to (empty = all endpoints)
    /// Supports wildcards: /api/orders/*
    /// </summary>
    public List<string> Endpoints { get; set; } = new();

    /// <summary>
    /// Header name for idempotency key (default: X-Idempotency-Key)
    /// </summary>
    public string HeaderName { get; set; } = "X-Idempotency-Key";

    /// <summary>
    /// Whether to include original request timestamp in response
    /// </summary>
    public bool IncludeTimestampHeader { get; set; } = true;

    /// <summary>
    /// Response header name for original request timestamp
    /// </summary>
    public string TimestampHeaderName { get; set; } = "X-Idempotency-Replayed-At";
}
