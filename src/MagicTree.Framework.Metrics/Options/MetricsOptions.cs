namespace MagicTree.Framework.Metrics.Options;

/// <summary>
/// Configuration options for Prometheus metrics
/// </summary>
public class MetricsOptions
{
    public static string SectionName => "Metrics";
    
    /// <summary>
    /// Enable or disable metrics collection
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Endpoint path for Prometheus scraping (default: /metrics)
    /// </summary>
    public string EndpointPath { get; set; } = "/metrics";
    
    /// <summary>
    /// Application name for metric labels
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;
    
    /// <summary>
    /// Environment name for metric labels (Production, Staging, Development)
    /// </summary>
    public string Environment { get; set; } = "Development";
}
