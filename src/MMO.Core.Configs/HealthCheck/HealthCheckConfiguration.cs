using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace MMO.Core.Configs.HealthCheck;

public static class HealthCheckConfiguration
{
    private const string DefaultHealthCheckPath = "/health";
    private const string DefaultReadyCheckPath = "/ready";
    private const string DefaultLiveCheckPath = "/live";

    public static IServiceCollection AddHealthCheckConfiguration(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });
        
        
        return services;
    }

    public static async Task<IApplicationBuilder> ConfigureHealthCheckEndpoints(
        this WebApplication app,
        string healthCheckPath = DefaultHealthCheckPath,
        string readyCheckPath = DefaultReadyCheckPath,
        string liveCheckPath = DefaultLiveCheckPath)
    {
        // Liveness probe - checks if the app is running
        app.MapHealthChecks(liveCheckPath, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteHealthCheckResponse
        });

        // Readiness probe - checks if the app is ready to serve requests
        app.MapHealthChecks(readyCheckPath, new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = WriteHealthCheckResponse
        });

        // General health check
        app.MapHealthChecks(healthCheckPath, new HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponse
        });
        return app;
    }

    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration,
                exception = e.Value.Exception?.Message,
                data = e.Value.Data
            })
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(result);
    }
}
