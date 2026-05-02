using System;

namespace MagicTree.Framework.Dtos;

public record BasedResponseDto
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = null!;
    public StatusCode StatusCode { get; set; } = StatusCode.InternalServerError;

    /// <summary>
    /// Optional metadata for additional context.
    /// Example: Pagination info, cache status, execution time.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Optional trace ID for debugging and log correlation.
    /// Useful for tracking errors across microservices.
    /// </summary>
    public string? TraceId { get; set; }
}
