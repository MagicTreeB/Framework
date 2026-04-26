namespace MMO.Core.BlobStorage.Models;

/// <summary>
/// Request to upload a blob
/// </summary>
public class BlobUploadRequest
{
    /// <summary>
    /// Original filename from the user
    /// </summary>
    public required string OriginalFileName { get; init; }

    /// <summary>
    /// Content type (MIME type)
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Stream containing file data
    /// </summary>
    public required Stream FileStream { get; init; }

    /// <summary>
    /// Optional bucket name (uses default if not specified)
    /// </summary>
    public string? BucketName { get; init; }

    /// <summary>
    /// Optional metadata tags
    /// </summary>
    public Dictionary<string, string>? Tags { get; init; }

    /// <summary>
    /// Whether to generate a public URL (default: false)
    /// </summary>
    public bool GeneratePublicUrl { get; init; } = false;
}
