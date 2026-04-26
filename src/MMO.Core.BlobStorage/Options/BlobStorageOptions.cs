namespace MMO.Core.BlobStorage.Options;

/// <summary>
/// Configuration options for blob storage
/// </summary>
public class BlobStorageOptions
{
    public static string SectionName => "BlobStorage";

    /// <summary>
    /// MinIO endpoint (e.g., "localhost:9000")
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// Access key for MinIO
    /// </summary>
    public required string AccessKey { get; set; }

    /// <summary>
    /// Secret key for MinIO
    /// </summary>
    public required string SecretKey { get; set; }

    /// <summary>
    /// Whether to use SSL/TLS (default: false for local dev)
    /// </summary>
    public bool UseSSL { get; set; } = false;

    /// <summary>
    /// Default bucket name for file storage
    /// </summary>
    public string DefaultBucket { get; set; } = "files";

    /// <summary>
    /// Whether to auto-create buckets if they don't exist
    /// </summary>
    public bool AutoCreateBuckets { get; set; } = true;

    /// <summary>
    /// Maximum file size in MB (default: 100MB)
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 100;

    /// <summary>
    /// Allowed file extensions (empty = all allowed)
    /// </summary>
    public List<string> AllowedExtensions { get; set; } = new();

    /// <summary>
    /// Presigned URL expiration in hours (default: 24 hours)
    /// </summary>
    public int PresignedUrlExpirationHours { get; set; } = 24;
}
