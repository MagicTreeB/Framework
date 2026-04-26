namespace MMO.Core.VideoStorage.Models;

/// <summary>
/// Storage usage statistics
/// </summary>
public class VideoStorageStats
{
    /// <summary>
    /// Total number of videos stored
    /// </summary>
    public int TotalVideoCount { get; init; }

    /// <summary>
    /// Total storage size in bytes
    /// </summary>
    public long TotalSizeInBytes { get; init; }

    /// <summary>
    /// Total storage size in MB
    /// </summary>
    public double TotalSizeInMB => TotalSizeInBytes / (1024.0 * 1024.0);

    /// <summary>
    /// Total storage size in GB
    /// </summary>
    public double TotalSizeInGB => TotalSizeInBytes / (1024.0 * 1024.0 * 1024.0);

    /// <summary>
    /// Average video size in bytes
    /// </summary>
    public long AverageSizeInBytes => TotalVideoCount > 0 ? TotalSizeInBytes / TotalVideoCount : 0;

    /// <summary>
    /// Timestamp when stats were calculated
    /// </summary>
    public DateTimeOffset CalculatedAt { get; init; } = DateTimeOffset.UtcNow;
}
