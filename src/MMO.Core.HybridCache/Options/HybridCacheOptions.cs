namespace MMO.Core.HybridCache.Options;

/// <summary>
/// Configuration options for HybridCache
/// </summary>
public class HybridCacheConfig
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public static string SectionName => "HybridCache";

    /// <summary>
    /// Maximum size of cached payloads in bytes (default: 1 MB)
    /// </summary>
    public long MaximumPayloadBytes { get; set; } = 1024 * 1024;

    /// <summary>
    /// Maximum length of cache keys (default: 512)
    /// </summary>
    public int MaximumKeyLength { get; set; } = 512;

    /// <summary>
    /// Default expiration time for L2 cache in minutes (default: 5)
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// Default expiration time for L1 cache in minutes (default: 1)
    /// </summary>
    public int LocalCacheExpirationMinutes { get; set; } = 1;
}
