using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MagicTree.Framework.HybridCache.Options;
using MagicTree.Framework.HybridCache.Services;

namespace MagicTree.Framework.HybridCache.Extensions;

/// <summary>
/// Extension methods for registering HybridCache services
/// </summary>
public static class HybridCacheExtensions
{
    /// <summary>
    /// Add HybridCache service with configuration from appsettings
    /// </summary>
    public static IServiceCollection AddHybridCacheService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind options
        var options = new HybridCacheConfig();
        configuration.GetSection(HybridCacheConfig.SectionName).Bind(options);
        services.Configure<HybridCacheConfig>(configuration.GetSection(HybridCacheConfig.SectionName));

        // Register HybridCache with options
        services.AddHybridCache(hybridOptions =>
        {
            hybridOptions.MaximumPayloadBytes = options.MaximumPayloadBytes;
            hybridOptions.MaximumKeyLength = options.MaximumKeyLength;
            hybridOptions.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(options.DefaultExpirationMinutes),
                LocalCacheExpiration = TimeSpan.FromMinutes(options.LocalCacheExpirationMinutes)
            };
        });

        // Register CacheService wrapper
        services.AddScoped<CacheService>();

        return services;
    }

    /// <summary>
    /// Add HybridCache service with programmatic configuration
    /// </summary>
    public static IServiceCollection AddHybridCacheService(
        this IServiceCollection services,
        Action<HybridCacheConfig> configureOptions)
    {
        var options = new HybridCacheConfig();
        configureOptions(options);
        services.Configure(configureOptions);

        // Register HybridCache with options
        services.AddHybridCache(hybridOptions =>
        {
            hybridOptions.MaximumPayloadBytes = options.MaximumPayloadBytes;
            hybridOptions.MaximumKeyLength = options.MaximumKeyLength;
            hybridOptions.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(options.DefaultExpirationMinutes),
                LocalCacheExpiration = TimeSpan.FromMinutes(options.LocalCacheExpirationMinutes)
            };
        });

        // Register CacheService wrapper
        services.AddScoped<CacheService>();

        return services;
    }
}
