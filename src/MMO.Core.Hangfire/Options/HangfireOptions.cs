namespace MMO.Core.Hangfire.Options;

/// <summary>
/// Configuration options for Hangfire background job processing
/// </summary>
public class HangfireOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public static string SectionName => "Hangfire";

    /// <summary>
    /// Enable or disable Hangfire. Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Storage type: SqlServer, InMemory. Default: SqlServer
    /// </summary>
    public string StorageType { get; set; } = "SqlServer";

    /// <summary>
    /// SQL Server connection string for Hangfire storage
    /// Required when StorageType = SqlServer
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Dashboard configuration
    /// </summary>
    public DashboardOptions Dashboard { get; set; } = new();

    /// <summary>
    /// Worker configuration for job processing
    /// </summary>
    public WorkerOptions Worker { get; set; } = new();

    /// <summary>
    /// Retry policy configuration
    /// </summary>
    public RetryOptions Retry { get; set; } = new();
}

/// <summary>
/// Hangfire dashboard configuration
/// </summary>
public class DashboardOptions
{
    /// <summary>
    /// Enable dashboard UI. Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Dashboard route path. Default: /hangfire
    /// </summary>
    public string Route { get; set; } = "/hangfire";

    /// <summary>
    /// Require authentication to access dashboard. Default: true
    /// </summary>
    public bool RequireAuthentication { get; set; } = true;

    /// <summary>
    /// Required role to access dashboard. Default: Admin
    /// </summary>
    public string RequiredRole { get; set; } = "Admin";

    /// <summary>
    /// Dashboard title displayed in UI. Default: Hangfire Dashboard
    /// </summary>
    public string Title { get; set; } = "Hangfire Dashboard";
}

/// <summary>
/// Worker configuration for background job processing
/// </summary>
public class WorkerOptions
{
    /// <summary>
    /// Number of concurrent worker threads. Default: 20
    /// Adjust based on expected job volume and server resources
    /// </summary>
    public int WorkerCount { get; set; } = 20;

    /// <summary>
    /// Queue names to process. Default: ["default"]
    /// Multiple queues allow job prioritization
    /// </summary>
    public string[] Queues { get; set; } = new[] { "default" };

    /// <summary>
    /// Job polling interval in seconds. Default: 15
    /// Lower = more responsive, higher = less DB load
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 15;
}

/// <summary>
/// Retry policy for failed jobs
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// Maximum number of automatic retry attempts. Default: 10
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 10;

    /// <summary>
    /// Enable exponential backoff for retries. Default: true
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Initial delay before first retry in seconds. Default: 60
    /// </summary>
    public int InitialDelaySeconds { get; set; } = 60;
}
