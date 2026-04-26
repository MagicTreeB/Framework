namespace MMO.Core.VideoStorage.Models;

/// <summary>
/// Paginated result of video listing
/// </summary>
public class VideoListResult
{
    /// <summary>
    /// List of video metadata
    /// </summary>
    public required List<VideoMetadata> Videos { get; init; }

    /// <summary>
    /// Total number of videos in storage
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
}
