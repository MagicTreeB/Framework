using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MMO.Core.Hangfire.Interfaces;
using MMO.Core.Hangfire.Options;
using MMO.Core.Hangfire.Services;

namespace MMO.Core.Hangfire.Extensions;

/// <summary>
/// Extension methods for configuring Hangfire services
/// </summary>
public static class HangfireExtensions
{
    /// <summary>
    /// Add Hangfire services with configuration from appsettings.json
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration containing Hangfire section</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddHangfireServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(HangfireOptions.SectionName).Get<HangfireOptions>()
            ?? new HangfireOptions();

        if (!options.Enabled)
        {
            return services;
        }

        // Register options
        services.Configure<HangfireOptions>(configuration.GetSection(HangfireOptions.SectionName));

        // Configure Hangfire storage
        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
            config.UseSimpleAssemblyNameTypeSerializer();
            config.UseRecommendedSerializerSettings();

            if (options.StorageType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(options.ConnectionString))
                {
                    throw new InvalidOperationException(
                        "Hangfire ConnectionString is required when StorageType is SqlServer");
                }

                config.UseSqlServerStorage(options.ConnectionString, new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.FromSeconds(options.Worker.PollingIntervalSeconds),
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true,
                    SchemaName = "Hangfire"
                });
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported Hangfire StorageType: {options.StorageType}. Only 'SqlServer' is currently supported.");
            }
        });

        // Add Hangfire server with worker configuration
        services.AddHangfireServer(serverOptions =>
        {
            serverOptions.WorkerCount = options.Worker.WorkerCount;
            serverOptions.Queues = options.Worker.Queues;
            serverOptions.SchedulePollingInterval = TimeSpan.FromSeconds(options.Worker.PollingIntervalSeconds);
        });

        // Register job service
        services.AddScoped<IJobService, JobService>();

        return services;
    }

    /// <summary>
    /// Use Hangfire dashboard with authentication and authorization
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <param name="configuration">Configuration containing Hangfire section</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseHangfireDashboardWithAuth(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(HangfireOptions.SectionName).Get<HangfireOptions>()
            ?? new HangfireOptions();

        if (!options.Enabled || !options.Dashboard.Enabled)
        {
            return app;
        }

        var dashboardOptions = new global::Hangfire.DashboardOptions
        {
            DashboardTitle = options.Dashboard.Title,
            Authorization = options.Dashboard.RequireAuthentication
                ? new[] { new HangfireAuthorizationFilter(options.Dashboard.RequiredRole) }
                : Array.Empty<IDashboardAuthorizationFilter>()
        };

        app.UseHangfireDashboard(options.Dashboard.Route, dashboardOptions);

        return app;
    }
}

/// <summary>
/// Authorization filter for Hangfire dashboard
/// </summary>
internal class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly string _requiredRole;

    public HangfireAuthorizationFilter(string requiredRole)
    {
        _requiredRole = requiredRole;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Check if user is authenticated
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        // Check if user has required role (if specified)
        if (!string.IsNullOrEmpty(_requiredRole))
        {
            return httpContext.User.IsInRole(_requiredRole);
        }

        // Allow authenticated users if no specific role required
        return true;
    }
}
