namespace MagicTree.Framework.SignalR.Options;

/// <summary>
/// Configuration options for SignalR
/// </summary>
public class SignalROptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public static string SectionName => "SignalR";

    /// <summary>
    /// Enable SignalR functionality
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Use Redis backplane for scaling (requires Redis connection string)
    /// </summary>
    public bool UseRedisBackplane { get; set; } = false;

    /// <summary>
    /// Redis connection string (required if UseRedisBackplane is true)
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Enable detailed error messages (development only)
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;

    /// <summary>
    /// Keep-alive interval in seconds (default: 15)
    /// </summary>
    public int KeepAliveIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Client timeout interval in seconds (default: 30)
    /// </summary>
    public int ClientTimeoutIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum message size in bytes (default: 32KB)
    /// </summary>
    public long MaximumReceiveMessageSize { get; set; } = 32 * 1024;

    /// <summary>
    /// Streaming buffer capacity (default: 10)
    /// </summary>
    public int StreamingBufferCapacity { get; set; } = 10;

    /// <summary>
    /// Enable automatic reconnection
    /// </summary>
    public bool EnableReconnect { get; set; } = true;

    /// <summary>
    /// CORS allowed origins (comma-separated)
    /// </summary>
    public string AllowedOrigins { get; set; } = "*";
}
