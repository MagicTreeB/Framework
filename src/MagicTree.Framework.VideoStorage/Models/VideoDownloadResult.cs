namespace MagicTree.Framework.VideoStorage.Models;

/// <summary>
/// Result of a video download operation
/// </summary>
public class VideoDownloadResult : IDisposable
{
    /// <summary>
    /// Video metadata
    /// </summary>
    public required VideoMetadata Metadata { get; init; }

    /// <summary>
    /// Video file stream
    /// </summary>
    public required Stream VideoStream { get; init; }

    /// <summary>
    /// Whether the stream should be disposed when this object is disposed
    /// </summary>
    public bool LeaveStreamOpen { get; init; } = false;

    public void Dispose()
    {
        if (!LeaveStreamOpen)
        {
            VideoStream?.Dispose();
        }
    }
}
