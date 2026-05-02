namespace MagicTree.Framework.Idempotency.Models;

/// <summary>
/// Represents a stored idempotency record
/// </summary>
public class IdempotencyRecord
{
    /// <summary>
    /// Unique idempotency key
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code of the original response
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Response headers from the original response
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Response body from the original response (as base64 for binary data)
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Content type of the original response
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// When the original request was processed
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When this record will expire
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Whether the request is currently being processed (for conflict detection)
    /// </summary>
    public bool IsProcessing { get; set; }

    /// <summary>
    /// Request method (POST, PUT, etc.)
    /// </summary>
    public string RequestMethod { get; set; } = string.Empty;

    /// <summary>
    /// Request path
    /// </summary>
    public string RequestPath { get; set; } = string.Empty;
}
