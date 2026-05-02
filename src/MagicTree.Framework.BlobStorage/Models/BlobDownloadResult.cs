namespace MagicTree.Framework.BlobStorage.Models;

/// <summary>
/// Result of a blob download operation
/// </summary>
public class BlobDownloadResult : IDisposable
{
    /// <summary>
    /// Blob metadata
    /// </summary>
    public required BlobMetadata Metadata { get; init; }

    /// <summary>
    /// Stream containing the blob content
    /// </summary>
    public required Stream Content { get; init; }

    /// <summary>
    /// Content type for HTTP response headers
    /// </summary>
    public string ContentType => Metadata.ContentType;

    /// <summary>
    /// Suggested filename for download (Content-Disposition header)
    /// </summary>
    public string FileName => Metadata.OriginalFileName;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize => Metadata.SizeInBytes;

    public void Dispose()
    {
        Content?.Dispose();
    }
}
