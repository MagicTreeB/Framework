using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MMO.Core.RateLimit.Interfaces;
using MMO.Core.RateLimit.Middlewares;
using MMO.Core.RateLimit.Options;
using MMO.Core.RateLimit.Services;
using MMO.Core.RateLimit.Storage;
using StackExchange.Redis;
using System.Text.Json;

namespace MMO.Core.RateLimit.Extensions;

/// <summary>
/// Extension methods for rate limiting configuration
/// </summary>
public static class RateLimitExtensions
{
    /// <summary>
    /// Add rate limiting services to the DI container
    /// </summary>
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure options from configuration section
        services.Configure<RateLimitOptions>(options =>
        {
            configuration.GetSection(RateLimitOptions.SectionName).Bind(options);
        });

        // Get configuration for service registration logic
        var section = configuration.GetSection(RateLimitOptions.SectionName);
        var options = new RateLimitOptions
        {
            Enabled = section.GetValue<bool>("Enabled"),
            StorageType = section.GetValue<string>("StorageType") ?? "InMemory",
            RedisConnectionString = section.GetValue<string>("RedisConnectionString") ?? string.Empty
        };

        // Register storage based on configuration
        if (options.StorageType.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(options.RedisConnectionString))
            {
                throw new InvalidOperationException(
                    "Redis connection string is required when StorageType is 'Redis'");
            }

            // Register Redis
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(options.RedisConnectionString));

            services.AddSingleton<IRateLimitStorage, RedisRateLimitStorage>();
        }
        else
        {
            // Use in-memory storage
            services.AddMemoryCache();
            services.AddSingleton<IRateLimitStorage, InMemoryRateLimitStorage>();
        }

        // Register rate limit service
        services.AddSingleton<IRateLimitService, RateLimitService>();

        return services;
    }

    /// <summary>
    /// Add rate limiting services with custom options
    /// </summary>
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        Action<RateLimitOptions> configure)
    {
        services.Configure(configure);

        var options = new RateLimitOptions();
        configure(options);

        // Register storage
        if (options.StorageType.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(options.RedisConnectionString))
            {
                throw new InvalidOperationException(
                    "Redis connection string is required when StorageType is 'Redis'");
            }

            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(options.RedisConnectionString));

            services.AddSingleton<IRateLimitStorage, RedisRateLimitStorage>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddSingleton<IRateLimitStorage, InMemoryRateLimitStorage>();
        }

        services.AddSingleton<IRateLimitService, RateLimitService>();

        return services;
    }

    /// <summary>
    /// Use rate limiting middleware
    /// </summary>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitMiddleware>();
    }
}
