using MagicTree.Framework.VideoStorage.Models;

namespace MagicTree.Framework.VideoStorage.Interfaces;

/// <summary>
/// Core video storage interface for GUID-based video file management
/// </summary>
public interface IVideoStorage
{
    /// <summary>
    /// Upload a video file and generate a GUID as its identifier
    /// </summary>
    /// <param name="request">Upload request with video stream and metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Video metadata including the generated GUID</returns>
    Task<VideoMetadata> UploadAsync(VideoUploadRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a video file by its GUID
    /// </summary>
    /// <param name="videoId">GUID identifier of the video</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Video download result with stream and metadata</returns>
    Task<VideoDownloadResult?> DownloadAsync(Guid videoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a video file by its GUID
    /// </summary>
    /// <param name="videoId">GUID identifier of the video</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    Task<bool> DeleteAsync(Guid videoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a video exists by its GUID
    /// </summary>
    /// <param name="videoId">GUID identifier of the video</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if video exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid videoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get video metadata without downloading the file
    /// </summary>
    /// <param name="videoId">GUID identifier of the video</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Video metadata if found, null otherwise</returns>
    Task<VideoMetadata?> GetMetadataAsync(Guid videoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// List all videos in storage
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of video metadata</returns>
    Task<VideoListResult> ListAsync(int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get storage statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Storage usage statistics</returns>
    Task<VideoStorageStats> GetStorageStatsAsync(CancellationToken cancellationToken = default);
}
