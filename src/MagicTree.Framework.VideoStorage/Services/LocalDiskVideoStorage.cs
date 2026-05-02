using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MagicTree.Framework.VideoStorage.Interfaces;
using MagicTree.Framework.VideoStorage.Models;
using MagicTree.Framework.VideoStorage.Options;
using System.Text.Json;

namespace MagicTree.Framework.VideoStorage.Services;

/// <summary>
/// Local disk implementation of video storage with GUID-based file management
/// </summary>
public class LocalDiskVideoStorage : IVideoStorage
{
    private readonly VideoStorageOptions _options;
    private readonly ILogger<LocalDiskVideoStorage> _logger;
    private const string MetadataFileExtension = ".meta.json";

    public LocalDiskVideoStorage(
        IOptions<VideoStorageOptions> options,
        ILogger<LocalDiskVideoStorage> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Ensure storage directory exists
        EnsureStorageDirectoryExists();
    }

    public async Task<VideoMetadata> UploadAsync(VideoUploadRequest request, CancellationToken cancellationToken = default)
    {
        var videoId = Guid.NewGuid();
        var fileExtension = Path.GetExtension(request.OriginalFileName);

        // Validate file size
        if (request.VideoStream.Length > _options.MaxFileSizeMB * 1024L * 1024L)
        {
            throw new InvalidOperationException($"Video size exceeds maximum allowed size of {_options.MaxFileSizeMB}MB");
        }

        // Validate file extension
        if (_options.AllowedExtensions.Any() && !string.IsNullOrEmpty(fileExtension))
        {
            if (!_options.AllowedExtensions.Contains(fileExtension.ToLowerInvariant()))
            {
                throw new InvalidOperationException($"File extension '{fileExtension}' is not allowed");
            }
        }

        try
        {
            // Determine storage path
            var storagePath = GetStoragePath(videoId, fileExtension);
            var directory = Path.GetDirectoryName(storagePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write video file to disk
            await using (var fileStream = new FileStream(storagePath, FileMode.Create, FileAccess.Write, FileShare.None, _options.BufferSize, useAsync: true))
            {
                await request.VideoStream.CopyToAsync(fileStream, _options.BufferSize, cancellationToken);
                await fileStream.FlushAsync(cancellationToken);
            }

            // Get file info
            var fileInfo = new FileInfo(storagePath);

            // Create metadata
            var metadata = new VideoMetadata
            {
                VideoId = videoId,
                OriginalFileName = request.OriginalFileName,
                ContentType = request.ContentType,
                SizeInBytes = fileInfo.Length,
                FileExtension = fileExtension,
                FilePath = storagePath,
                PublicUrl = GeneratePublicUrl(videoId, fileExtension),
                UploadedAt = DateTimeOffset.UtcNow,
                DurationSeconds = request.DurationSeconds,
                Width = request.Width,
                Height = request.Height,
                Title = request.Title,
                Description = request.Description,
                Tags = request.Tags,
                UserId = request.UserId
            };

            // Save metadata file if enabled
            if (_options.CreateMetadataFile)
            {
                await SaveMetadataFileAsync(metadata, cancellationToken);
            }

            _logger.LogInformation("Video uploaded successfully: {VideoId}, Size: {Size} bytes, Path: {Path}",
                videoId, fileInfo.Length, storagePath);

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload video: {VideoId}, OriginalFileName: {FileName}",
                videoId, request.OriginalFileName);
            throw;
        }
    }

    public async Task<VideoDownloadResult?> DownloadAsync(Guid videoId, CancellationToken cancellationToken = default)
    {
        var metadata = await GetMetadataAsync(videoId, cancellationToken);
        if (metadata == null)
        {
            _logger.LogWarning("Video not found: {VideoId}", videoId);
            return null;
        }

        if (!File.Exists(metadata.FilePath))
        {
            _logger.LogError("Video file not found on disk: {VideoId}, Path: {Path}", videoId, metadata.FilePath);
            return null;
        }

        try
        {
            var fileStream = new FileStream(metadata.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, _options.BufferSize, useAsync: true);

            return new VideoDownloadResult
            {
                Metadata = metadata,
                VideoStream = fileStream,
                LeaveStreamOpen = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download video: {VideoId}", videoId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid videoId, CancellationToken cancellationToken = default)
    {
        var metadata = await GetMetadataAsync(videoId, cancellationToken);
        if (metadata == null)
        {
            _logger.LogWarning("Video not found for deletion: {VideoId}", videoId);
            return false;
        }

        try
        {
            // Delete video file
            if (File.Exists(metadata.FilePath))
            {
                File.Delete(metadata.FilePath);
            }

            // Delete metadata file
            var metadataPath = GetMetadataFilePath(videoId);
            if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
            }

            _logger.LogInformation("Video deleted successfully: {VideoId}", videoId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete video: {VideoId}", videoId);
            throw;
        }
    }

    public Task<bool> ExistsAsync(Guid videoId, CancellationToken cancellationToken = default)
    {
        var metadataPath = GetMetadataFilePath(videoId);
        var exists = File.Exists(metadataPath);
        return Task.FromResult(exists);
    }

    public async Task<VideoMetadata?> GetMetadataAsync(Guid videoId, CancellationToken cancellationToken = default)
    {
        if (!_options.CreateMetadataFile)
        {
            _logger.LogWarning("Metadata file creation is disabled. Cannot retrieve metadata for {VideoId}", videoId);
            return null;
        }

        var metadataPath = GetMetadataFilePath(videoId);
        if (!File.Exists(metadataPath))
        {
            _logger.LogDebug("Metadata file not found: {VideoId}", videoId);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(metadataPath, cancellationToken);
            var metadata = JsonSerializer.Deserialize<VideoMetadata>(json);
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read metadata for video: {VideoId}", videoId);
            return null;
        }
    }

    public async Task<VideoListResult> ListAsync(int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!_options.CreateMetadataFile)
        {
            _logger.LogWarning("Metadata file creation is disabled. Cannot list videos.");
            return new VideoListResult
            {
                Videos = new List<VideoMetadata>(),
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        try
        {
            var metadataFiles = Directory.GetFiles(_options.StorageDirectory, $"*{MetadataFileExtension}", SearchOption.AllDirectories);
            var allMetadata = new List<VideoMetadata>();

            foreach (var metadataFile in metadataFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(metadataFile, cancellationToken);
                    var metadata = JsonSerializer.Deserialize<VideoMetadata>(json);
                    if (metadata != null)
                    {
                        allMetadata.Add(metadata);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read metadata file: {FilePath}", metadataFile);
                }
            }

            // Sort by upload date descending
            var sortedMetadata = allMetadata.OrderByDescending(m => m.UploadedAt).ToList();

            // Apply pagination
            var paginatedMetadata = sortedMetadata
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new VideoListResult
            {
                Videos = paginatedMetadata,
                TotalCount = sortedMetadata.Count,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list videos");
            throw;
        }
    }

    public async Task<VideoStorageStats> GetStorageStatsAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.CreateMetadataFile)
        {
            _logger.LogWarning("Metadata file creation is disabled. Cannot calculate stats.");
            return new VideoStorageStats
            {
                TotalVideoCount = 0,
                TotalSizeInBytes = 0,
                CalculatedAt = DateTimeOffset.UtcNow
            };
        }

        try
        {
            var metadataFiles = Directory.GetFiles(_options.StorageDirectory, $"*{MetadataFileExtension}", SearchOption.AllDirectories);
            long totalSize = 0;
            int count = 0;

            foreach (var metadataFile in metadataFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(metadataFile, cancellationToken);
                    var metadata = JsonSerializer.Deserialize<VideoMetadata>(json);
                    if (metadata != null)
                    {
                        totalSize += metadata.SizeInBytes;
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read metadata file: {FilePath}", metadataFile);
                }
            }

            return new VideoStorageStats
            {
                TotalVideoCount = count,
                TotalSizeInBytes = totalSize,
                CalculatedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate storage stats");
            throw;
        }
    }

    #region Private Helper Methods

    private void EnsureStorageDirectoryExists()
    {
        if (!Directory.Exists(_options.StorageDirectory))
        {
            Directory.CreateDirectory(_options.StorageDirectory);
            _logger.LogInformation("Created storage directory: {Directory}", _options.StorageDirectory);
        }
    }

    private string GetStoragePath(Guid videoId, string fileExtension)
    {
        if (_options.OrganizeByDate)
        {
            var dateFolder = DateTimeOffset.UtcNow.ToString(_options.DateFormat);
            var directory = Path.Combine(_options.StorageDirectory, dateFolder);
            return Path.Combine(directory, $"{videoId}{fileExtension}");
        }

        return Path.Combine(_options.StorageDirectory, $"{videoId}{fileExtension}");
    }

    private string GetMetadataFilePath(Guid videoId)
    {
        // Search for metadata file in all subdirectories
        var metadataPattern = $"{videoId}{MetadataFileExtension}";
        var files = Directory.GetFiles(_options.StorageDirectory, metadataPattern, SearchOption.AllDirectories);
        
        if (files.Length > 0)
        {
            return files[0];
        }

        // If not found, return default path
        if (_options.OrganizeByDate)
        {
            var dateFolder = DateTimeOffset.UtcNow.ToString(_options.DateFormat);
            var directory = Path.Combine(_options.StorageDirectory, dateFolder);
            return Path.Combine(directory, metadataPattern);
        }

        return Path.Combine(_options.StorageDirectory, metadataPattern);
    }

    private async Task SaveMetadataFileAsync(VideoMetadata metadata, CancellationToken cancellationToken)
    {
        var metadataPath = Path.Combine(Path.GetDirectoryName(metadata.FilePath)!, $"{metadata.VideoId}{MetadataFileExtension}");
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metadataPath, json, cancellationToken);
    }

    private string? GeneratePublicUrl(Guid videoId, string fileExtension)
    {
        if (string.IsNullOrEmpty(_options.BaseUrl))
        {
            return null;
        }

        return $"{_options.BaseUrl.TrimEnd('/')}/videos/{videoId}{fileExtension}";
    }

    #endregion
}
