namespace MagicTree.Framework.VideoStorage.Models;

/// <summary>
/// Metadata information for a video
/// </summary>
public class VideoMetadata
{
    /// <summary>
    /// Unique identifier for the video (GUID)
    /// </summary>
    public required Guid VideoId { get; init; }

    /// <summary>
    /// Original filename uploaded by the user
    /// </summary>
    public required string OriginalFileName { get; init; }

    /// <summary>
    /// Content type (MIME type) of the video
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Size of the video in bytes
    /// </summary>
    public long SizeInBytes { get; init; }

    /// <summary>
    /// File extension (e.g., ".mp4", ".webm")
    /// </summary>
    public string? FileExtension { get; init; }

    /// <summary>
    /// Physical file path on disk
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Optional public URL to access the video
    /// </summary>
    public string? PublicUrl { get; init; }

    /// <summary>
    /// Timestamp when the video was uploaded
    /// </summary>
    public DateTimeOffset UploadedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Duration of the video in seconds
    /// </summary>
    public double? DurationSeconds { get; init; }

    /// <summary>
    /// Video width in pixels
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Video height in pixels
    /// </summary>
    public int? Height { get; init; }

    /// <summary>
    /// Optional title for the video
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Optional description of the video
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Additional metadata tags
    /// </summary>
    public Dictionary<string, string>? Tags { get; init; }

    /// <summary>
    /// User ID who uploaded the video
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Thumbnail URL (if generated)
    /// </summary>
    public string? ThumbnailUrl { get; init; }
}
