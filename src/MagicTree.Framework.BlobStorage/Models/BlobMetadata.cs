namespace MagicTree.Framework.BlobStorage.Models;

/// <summary>
/// Metadata information for a blob
/// </summary>
public class BlobMetadata
{
    /// <summary>
    /// Unique identifier for the blob (GUID)
    /// </summary>
    public required Guid BlobId { get; init; }

    /// <summary>
    /// Original filename uploaded by the user
    /// </summary>
    public required string OriginalFileName { get; init; }

    /// <summary>
    /// Content type (MIME type) of the blob
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Size of the blob in bytes
    /// </summary>
    public long SizeInBytes { get; init; }

    /// <summary>
    /// File extension (e.g., ".jpg", ".pdf")
    /// </summary>
    public string? FileExtension { get; init; }

    /// <summary>
    /// Public URL to access the blob (if available)
    /// </summary>
    public string? PublicUrl { get; init; }

    /// <summary>
    /// Bucket name where the blob is stored
    /// </summary>
    public required string BucketName { get; init; }

    /// <summary>
    /// Timestamp when the blob was uploaded
    /// </summary>
    public DateTimeOffset UploadedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional metadata tags
    /// </summary>
    public Dictionary<string, string>? Tags { get; init; }
}
