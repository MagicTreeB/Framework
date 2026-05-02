using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using MagicTree.Framework.BlobStorage.Interfaces;
using MagicTree.Framework.BlobStorage.Models;
using MagicTree.Framework.BlobStorage.Options;

namespace MagicTree.Framework.BlobStorage.Services;

/// <summary>
/// MinIO implementation of blob storage with GUID-based file management
/// </summary>
public class MinioBlobStorageService : IBlobStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly BlobStorageOptions _options;
    private readonly ILogger<MinioBlobStorageService> _logger;

    public MinioBlobStorageService(
        IMinioClient minioClient,
        IOptions<BlobStorageOptions> options,
        ILogger<MinioBlobStorageService> logger)
    {
        _minioClient = minioClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BlobMetadata> UploadAsync(BlobUploadRequest request, CancellationToken cancellationToken = default)
    {
        var blobId = Guid.NewGuid();
        var bucketName = request.BucketName ?? _options.DefaultBucket;
        var fileExtension = Path.GetExtension(request.OriginalFileName);
        var objectName = GenerateObjectName(blobId, fileExtension);

        // Ensure bucket exists
        await EnsureBucketExistsAsync(bucketName, cancellationToken);

        // Validate file size
        if (request.FileStream.Length > _options.MaxFileSizeMB * 1024 * 1024)
        {
            throw new InvalidOperationException($"File size exceeds maximum allowed size of {_options.MaxFileSizeMB}MB");
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
            // Upload file to MinIO
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(request.FileStream)
                .WithObjectSize(request.FileStream.Length)
                .WithContentType(request.ContentType);

            // Add metadata tags
            if (request.Tags != null && request.Tags.Any())
            {
                var headers = new Dictionary<string, string>();
                foreach (var tag in request.Tags)
                {
                    headers[$"x-amz-meta-{tag.Key}"] = tag.Value;
                }
                putObjectArgs.WithHeaders(headers);
            }

            // Add original filename as metadata
            putObjectArgs.WithHeaders(new Dictionary<string, string>
            {
                { "x-amz-meta-original-filename", request.OriginalFileName },
                { "x-amz-meta-blob-id", blobId.ToString() },
                { "x-amz-meta-uploaded-at", DateTimeOffset.UtcNow.ToString("o") }
            });

            await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

            _logger.LogInformation("Uploaded blob {BlobId} with original filename {FileName} to bucket {Bucket}",
                blobId, request.OriginalFileName, bucketName);

            // Generate public URL if requested
            string? publicUrl = null;
            if (request.GeneratePublicUrl)
            {
                publicUrl = await GetPresignedUrlAsync(blobId, _options.PresignedUrlExpirationHours, bucketName, cancellationToken);
            }

            return new BlobMetadata
            {
                BlobId = blobId,
                OriginalFileName = request.OriginalFileName,
                ContentType = request.ContentType,
                SizeInBytes = request.FileStream.Length,
                FileExtension = fileExtension,
                BucketName = bucketName,
                PublicUrl = publicUrl,
                Tags = request.Tags,
                UploadedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload blob {BlobId} to bucket {Bucket}", blobId, bucketName);
            throw;
        }
    }

    public async Task<BlobDownloadResult?> DownloadAsync(Guid blobId, string? bucketName = null, CancellationToken cancellationToken = default)
    {
        bucketName ??= _options.DefaultBucket;

        try
        {
            // Get metadata first
            var metadata = await GetMetadataAsync(blobId, bucketName, cancellationToken);
            if (metadata == null)
            {
                _logger.LogWarning("Blob {BlobId} not found in bucket {Bucket}", blobId, bucketName);
                return null;
            }

            var objectName = GenerateObjectName(blobId, metadata.FileExtension);
            var memoryStream = new MemoryStream();

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                });

            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);

            _logger.LogInformation("Downloaded blob {BlobId} from bucket {Bucket}", blobId, bucketName);

            return new BlobDownloadResult
            {
                Metadata = metadata,
                Content = memoryStream
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob {BlobId} from bucket {Bucket}", blobId, bucketName);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid blobId, string? bucketName = null, CancellationToken cancellationToken = default)
    {
        bucketName ??= _options.DefaultBucket;

        try
        {
            // Check if blob exists first
            var metadata = await GetMetadataAsync(blobId, bucketName, cancellationToken);
            if (metadata == null)
            {
                _logger.LogWarning("Blob {BlobId} not found in bucket {Bucket} for deletion", blobId, bucketName);
                return false;
            }

            var objectName = GenerateObjectName(blobId, metadata.FileExtension);

            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);

            _logger.LogInformation("Deleted blob {BlobId} from bucket {Bucket}", blobId, bucketName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob {BlobId} from bucket {Bucket}", blobId, bucketName);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid blobId, string? bucketName = null, CancellationToken cancellationToken = default)
    {
        var metadata = await GetMetadataAsync(blobId, bucketName, cancellationToken);
        return metadata != null;
    }

    public async Task<BlobMetadata?> GetMetadataAsync(Guid blobId, string? bucketName = null, CancellationToken cancellationToken = default)
    {
        bucketName ??= _options.DefaultBucket;

        try
        {
            // Try common extensions first, then without extension
            var extensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".txt", ".docx", ".xlsx", "" };

            foreach (var ext in extensions)
            {
                var objectName = GenerateObjectName(blobId, ext);

                try
                {
                    var statObjectArgs = new StatObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName);

                    var stat = await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);

                    // Extract metadata from headers
                    var originalFileName = stat.MetaData?.GetValueOrDefault("x-amz-meta-original-filename") ?? objectName;
                    var uploadedAtStr = stat.MetaData?.GetValueOrDefault("x-amz-meta-uploaded-at");
                    var uploadedAt = DateTimeOffset.TryParse(uploadedAtStr, out var parsed) ? parsed : DateTimeOffset.UtcNow;

                    return new BlobMetadata
                    {
                        BlobId = blobId,
                        OriginalFileName = originalFileName,
                        ContentType = stat.ContentType,
                        SizeInBytes = stat.Size,
                        FileExtension = ext,
                        BucketName = bucketName,
                        UploadedAt = uploadedAt
                    };
                }
                catch
                {
                    // Try next extension
                    continue;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metadata for blob {BlobId} in bucket {Bucket}", blobId, bucketName);
            return null;
        }
    }

    public async Task<string?> GetPresignedUrlAsync(Guid blobId, int? expirationHours = null, string? bucketName = null, CancellationToken cancellationToken = default)
    {
        bucketName ??= _options.DefaultBucket;
        var expiration = expirationHours ?? _options.PresignedUrlExpirationHours;

        try
        {
            var metadata = await GetMetadataAsync(blobId, bucketName, cancellationToken);
            if (metadata == null)
            {
                _logger.LogWarning("Cannot generate presigned URL for non-existent blob {BlobId}", blobId);
                return null;
            }

            var objectName = GenerateObjectName(blobId, metadata.FileExtension);

            var presignedGetObjectArgs = new PresignedGetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithExpiry(expiration * 3600); // Convert hours to seconds

            var url = await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);

            _logger.LogInformation("Generated presigned URL for blob {BlobId} valid for {Hours} hours", blobId, expiration);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for blob {BlobId}", blobId);
            return null;
        }
    }

    /// <summary>
    /// Generate object name from GUID and extension
    /// Format: {guid}{extension}
    /// </summary>
    private static string GenerateObjectName(Guid blobId, string? extension)
    {
        return string.IsNullOrEmpty(extension)
            ? blobId.ToString()
            : $"{blobId}{extension}";
    }

    /// <summary>
    /// Ensure bucket exists, create if not
    /// </summary>
    private async Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken)
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
            var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);

            if (!exists && _options.AutoCreateBuckets)
            {
                var makeBucketArgs = new MakeBucketArgs().WithBucket(bucketName);
                await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);
                _logger.LogInformation("Created bucket {BucketName}", bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure bucket {BucketName} exists", bucketName);
            throw;
        }
    }
}
