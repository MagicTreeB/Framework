using MagicTree.Framework.BlobStorage.Models;

namespace MagicTree.Framework.BlobStorage.Interfaces;

/// <summary>
/// Core blob storage service interface for GUID-based file management
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Upload a file and generate a GUID as its identifier
    /// </summary>
    /// <param name="request">Upload request with file stream and metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Blob metadata including the generated GUID</returns>
    Task<BlobMetadata> UploadAsync(BlobUploadRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a file by its GUID
    /// </summary>
    /// <param name="blobId">GUID identifier of the file</param>
    /// <param name="bucketName">Optional bucket name (uses default if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Blob download result with stream and metadata</returns>
    Task<BlobDownloadResult?> DownloadAsync(Guid blobId, string? bucketName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file by its GUID
    /// </summary>
    /// <param name="blobId">GUID identifier of the file</param>
    /// <param name="bucketName">Optional bucket name (uses default if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    Task<bool> DeleteAsync(Guid blobId, string? bucketName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a blob exists by its GUID
    /// </summary>
    /// <param name="blobId">GUID identifier of the file</param>
    /// <param name="bucketName">Optional bucket name (uses default if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if blob exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid blobId, string? bucketName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get blob metadata without downloading the file
    /// </summary>
    /// <param name="blobId">GUID identifier of the file</param>
    /// <param name="bucketName">Optional bucket name (uses default if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Blob metadata or null if not found</returns>
    Task<BlobMetadata?> GetMetadataAsync(Guid blobId, string? bucketName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a presigned URL for temporary direct access (useful for downloads)
    /// </summary>
    /// <param name="blobId">GUID identifier of the file</param>
    /// <param name="expirationHours">URL expiration in hours (default from config)</param>
    /// <param name="bucketName">Optional bucket name (uses default if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Presigned URL or null if blob not found</returns>
    Task<string?> GetPresignedUrlAsync(Guid blobId, int? expirationHours = null, string? bucketName = null, CancellationToken cancellationToken = default);
}
