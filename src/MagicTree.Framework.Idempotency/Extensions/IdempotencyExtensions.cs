using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MagicTree.Framework.Idempotency.Interfaces;
using MagicTree.Framework.Idempotency.Middlewares;
using MagicTree.Framework.Idempotency.Options;
using MagicTree.Framework.Idempotency.Storage;
using StackExchange.Redis;

namespace MagicTree.Framework.Idempotency.Extensions;

/// <summary>
/// Extension methods for idempotency configuration
/// </summary>
public static class IdempotencyExtensions
{
    /// <summary>
    /// Add idempotency services to the DI container
    /// </summary>
    public static IServiceCollection AddIdempotency(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure options from configuration section
        services.Configure<IdempotencyOptions>(options =>
        {
            configuration.GetSection(IdempotencyOptions.SectionName).Bind(options);
        });

        // Get configuration for service registration logic
        var section = configuration.GetSection(IdempotencyOptions.SectionName);
        var options = new IdempotencyOptions
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

            // Register Redis (check if already registered from rate limiting)
            if (!services.Any(s => s.ServiceType == typeof(IConnectionMultiplexer)))
            {
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                    ConnectionMultiplexer.Connect(options.RedisConnectionString));
            }

            services.AddSingleton<IIdempotencyStorage, RedisIdempotencyStorage>();
        }
        else
        {
            // Use in-memory storage
            services.AddMemoryCache();
            services.AddSingleton<IIdempotencyStorage, InMemoryIdempotencyStorage>();
        }

        return services;
    }

    /// <summary>
    /// Add idempotency services with custom options
    /// </summary>
    public static IServiceCollection AddIdempotency(
        this IServiceCollection services,
        Action<IdempotencyOptions> configure)
    {
        services.Configure(configure);

        var options = new IdempotencyOptions();
        configure(options);

        // Register storage
        if (options.StorageType.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(options.RedisConnectionString))
            {
                throw new InvalidOperationException(
                    "Redis connection string is required when StorageType is 'Redis'");
            }

            // Check if Redis is already registered (from rate limiting)
            if (!services.Any(s => s.ServiceType == typeof(IConnectionMultiplexer)))
            {
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                    ConnectionMultiplexer.Connect(options.RedisConnectionString));
            }

            services.AddSingleton<IIdempotencyStorage, RedisIdempotencyStorage>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddSingleton<IIdempotencyStorage, InMemoryIdempotencyStorage>();
        }

        return services;
    }

    /// <summary>
    /// Use idempotency middleware
    /// </summary>
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app)
    {
        return app.UseMiddleware<IdempotencyMiddleware>();
    }
}
