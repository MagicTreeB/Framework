namespace MagicTree.Framework.Dtos;

public record BasedResponseDto
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = string.Empty;
    public StatusCode StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; set; }
    public string? TraceId { get; set; }
}
