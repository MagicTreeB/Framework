namespace MMO.Core.VideoStorage.Models;

/// <summary>
/// Request model for uploading a video
/// </summary>
public class VideoUploadRequest
{
    /// <summary>
    /// Original filename of the video
    /// </summary>
    public required string OriginalFileName { get; init; }

    /// <summary>
    /// Video file stream
    /// </summary>
    public required Stream VideoStream { get; init; }

    /// <summary>
    /// Content type (MIME type) of the video (e.g., "video/mp4", "video/webm")
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Optional description of the video
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional title for the video
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Duration of the video in seconds (optional, can be extracted during processing)
    /// </summary>
    public double? DurationSeconds { get; init; }

    /// <summary>
    /// Video width in pixels (optional)
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Video height in pixels (optional)
    /// </summary>
    public int? Height { get; init; }

    /// <summary>
    /// Additional metadata tags
    /// </summary>
    public Dictionary<string, string>? Tags { get; init; }

    /// <summary>
    /// User ID who uploaded the video
    /// </summary>
    public string? UserId { get; init; }
}
