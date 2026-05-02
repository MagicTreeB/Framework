using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MagicTree.Framework.Metrics.Options;
using Prometheus;

namespace MagicTree.Framework.Metrics.Extensions;

/// <summary>
/// Extension methods for registering Prometheus metrics
/// </summary>
public static class MetricsExtensions
{
    /// <summary>
    /// Registers Prometheus metrics services
    /// </summary>
    public static IServiceCollection AddMetricsService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var metricsOptions = configuration.GetSection(MetricsOptions.SectionName).Get<MetricsOptions>()
            ?? new MetricsOptions();
        
        services.Configure<MetricsOptions>(configuration.GetSection(MetricsOptions.SectionName));
        
        // prometheus-net doesn't require explicit service registration
        // Metrics are registered via middleware
        
        return services;
    }
    
    /// <summary>
    /// Configures the metrics endpoint for Prometheus scraping
    /// </summary>
    public static IApplicationBuilder UseMetrics(this IApplicationBuilder app, IConfiguration configuration)
    {
        var metricsOptions = configuration.GetSection(MetricsOptions.SectionName).Get<MetricsOptions>()
            ?? new MetricsOptions();
        
        if (!metricsOptions.Enabled)
        {
            return app;
        }
        
        // Add HTTP request metrics middleware
        app.UseHttpMetrics();
        
        // Expose /metrics endpoint for Prometheus
        app.UseMetricServer(metricsOptions.EndpointPath);
        
        return app;
    }
}
