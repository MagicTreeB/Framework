using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MagicTree.Framework.VideoStorage.Interfaces;
using MagicTree.Framework.VideoStorage.Options;
using MagicTree.Framework.VideoStorage.Services;

namespace MagicTree.Framework.VideoStorage.Extensions;

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
