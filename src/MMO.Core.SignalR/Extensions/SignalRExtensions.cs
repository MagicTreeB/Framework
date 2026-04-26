using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MMO.Core.SignalR.Hubs;
using MMO.Core.SignalR.Interfaces;
using MMO.Core.SignalR.Options;
using MMO.Core.SignalR.Services;

namespace MMO.Core.SignalR.Extensions;

/// <summary>
/// Extension methods for configuring SignalR
/// </summary>
public static class SignalRExtensions
{
    /// <summary>
    /// Add SignalR services with configuration
    /// </summary>
    public static IServiceCollection AddSignalRService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register options
        services.Configure<SignalROptions>(configuration.GetSection(SignalROptions.SectionName));
        var signalROptions = configuration.GetSection(SignalROptions.SectionName).Get<SignalROptions>() ?? new SignalROptions();

        if (!signalROptions.Enabled)
        {
            return services;
        }

        // Configure SignalR
        var signalRBuilder = services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = signalROptions.EnableDetailedErrors;
            options.KeepAliveInterval = TimeSpan.FromSeconds(signalROptions.KeepAliveIntervalSeconds);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(signalROptions.ClientTimeoutIntervalSeconds);
            options.MaximumReceiveMessageSize = signalROptions.MaximumReceiveMessageSize;
            options.StreamBufferCapacity = signalROptions.StreamingBufferCapacity;
        });

        // Note: Add .AddStackExchangeRedis() in your API's Program.cs if using Redis backplane
        // Example: builder.Services.AddSignalR().AddStackExchangeRedis(connectionString);

        // Register SignalR service
        services.AddScoped<ISignalRService, SignalRService>();

        return services;
    }

    /// <summary>
    /// Map SignalR hub endpoints
    /// </summary>
    public static IApplicationBuilder UseSignalREndpoints(
        this IApplicationBuilder app,
        string hubPath = "/hubs/notifications")
    {
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<BaseNotificationHub>(hubPath);
        });

        return app;
    }

    /// <summary>
    /// Add CORS for SignalR
    /// </summary>
    public static IServiceCollection AddSignalRCors(
        this IServiceCollection services,
        IConfiguration configuration,
        string policyName = "SignalRCorsPolicy")
    {
        var signalROptions = configuration.GetSection(SignalROptions.SectionName).Get<SignalROptions>() ?? new SignalROptions();

        if (!signalROptions.Enabled)
        {
            return services;
        }

        var origins = signalROptions.AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries);

        services.AddCors(options =>
        {
            options.AddPolicy(policyName, builder =>
            {
                if (signalROptions.AllowedOrigins == "*")
                {
                    builder.AllowAnyOrigin();
                }
                else
                {
                    builder.WithOrigins(origins);
                }

                builder.AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Get user identifier for SignalR (used for user-specific messages)
    /// </summary>
    public static string? GetUserIdentifier(this HubCallerContext context)
    {
        return context.User?.Identity?.Name 
            ?? context.User?.FindFirst("sub")?.Value 
            ?? context.User?.FindFirst("id")?.Value;
    }
}
