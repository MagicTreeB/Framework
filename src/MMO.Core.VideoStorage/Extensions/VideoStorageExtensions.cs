using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MMO.Core.VideoStorage.Interfaces;
using MMO.Core.VideoStorage.Options;
using MMO.Core.VideoStorage.Services;

namespace MMO.Core.VideoStorage.Extensions;

/// <summary>
/// Extension methods for configuring video storage services
/// </summary>
public static class VideoStorageExtensions
{
    /// <summary>
    /// Add local disk video storage services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddLocalDiskVideoStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register options
        services.Configure<VideoStorageOptions>(
            configuration.GetSection(VideoStorageOptions.SectionName));

        // Register video storage service
        services.AddScoped<IVideoStorage, LocalDiskVideoStorage>();

        return services;
    }

    /// <summary>
    /// Add local disk video storage services with custom options
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Action to configure options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddLocalDiskVideoStorage(
        this IServiceCollection services,
        Action<VideoStorageOptions> configureOptions)
    {
        // Register options with action
        services.Configure(configureOptions);

        // Register video storage service
        services.AddScoped<IVideoStorage, LocalDiskVideoStorage>();

        return services;
    }
}
