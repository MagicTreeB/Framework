using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using MMO.Core.BlobStorage.Interfaces;
using MMO.Core.BlobStorage.Options;
using MMO.Core.BlobStorage.Services;

namespace MMO.Core.BlobStorage.Extensions;

/// <summary>
/// Extension methods for registering blob storage services
/// </summary>
public static class BlobStorageExtensions
{
    /// <summary>
    /// Add blob storage services to dependency injection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddBlobStorage(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration
        var section = configuration.GetSection(BlobStorageOptions.SectionName);
        services.Configure<BlobStorageOptions>(opt => section.Bind(opt));

        var options = section.Get<BlobStorageOptions>();
        
        if (options == null || string.IsNullOrEmpty(options.Endpoint))
        {
            throw new InvalidOperationException(
                "BlobStorage configuration is missing or invalid. " +
                "Please configure the 'BlobStorage' section in appsettings.json");
        }

        // Register MinIO client
        services.AddMinio(configureClient => configureClient
            .WithEndpoint(options.Endpoint)
            .WithCredentials(options.AccessKey, options.SecretKey)
            .WithSSL(options.UseSSL)
            .Build());

        // Register blob storage service
        services.AddScoped<IBlobStorageService, MinioBlobStorageService>();

        return services;
    }

    /// <summary>
    /// Get blob storage options from configuration
    /// </summary>
    public static BlobStorageOptions GetBlobStorageOptions(this IConfiguration configuration)
    {
        return configuration.GetSection(BlobStorageOptions.SectionName).Get<BlobStorageOptions>()
            ?? throw new InvalidOperationException("BlobStorage configuration not found");
    }
}
