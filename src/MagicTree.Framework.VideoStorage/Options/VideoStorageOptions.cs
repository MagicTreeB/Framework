namespace MagicTree.Framework.VideoStorage.Options;

/// <summary>
/// Configuration options for video storage
/// </summary>
public class VideoStorageOptions
{
    public static string SectionName => "VideoStorage";

    /// <summary>
    /// Base directory for storing videos (default: "./videos")
    /// </summary>
    public string StorageDirectory { get; set; } = "./videos";

    /// <summary>
    /// Maximum file size in MB (default: 500 MB)
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 500;

    /// <summary>
    /// Allowed video file extensions (empty = allow all)
    /// </summary>
    public List<string> AllowedExtensions { get; set; } = new()
    {
        ".mp4",
        ".webm",
        ".avi",
        ".mov",
        ".mkv",
        ".flv",
        ".wmv"
    };

    /// <summary>
    /// Whether to organize files in subdirectories by date (default: true)
    /// </summary>
    public bool OrganizeByDate { get; set; } = true;

    /// <summary>
    /// Date format for organizing directories (default: "yyyy/MM/dd")
    /// </summary>
    public string DateFormat { get; set; } = "yyyy/MM/dd";

    /// <summary>
    /// Whether to create a metadata file for each video (default: true)
    /// </summary>
    public bool CreateMetadataFile { get; set; } = true;

    /// <summary>
    /// Base URL for accessing videos (optional)
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Buffer size for file operations in bytes (default: 81920 = 80 KB)
    /// </summary>
    public int BufferSize { get; set; } = 81920;
}
