using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Scalar.AspNetCore;

namespace MagicTree.Framework.Configs.Scalar;

public static class ScalarConfiguration
{
    private const string DefaultUrl = "docs";

    public static IServiceCollection AddScalarConfiguration(this IServiceCollection services)
    {
        services.AddOpenApi();
        services.AddFeatureManagement();
        return services;
    }

    /// <summary>
    /// Maps OpenAPI endpoint for Gateway access
    /// This allows both direct access (/openapi/v1.json) and Gateway access (/auth/openapi/v1.json)
    /// </summary>
    public static IApplicationBuilder MapGatewayOpenApi(this WebApplication app, string? basePath = null)
    {
        if (!string.IsNullOrEmpty(basePath))
        {
            app.MapOpenApi($"{basePath}/openapi/{{documentName}}.json");
        }
        return app;
    }

    public static async Task<IApplicationBuilder> ConfigureScalarEndpoints(
        this WebApplication app, 
        string title = "API Documentation",
        string url = DefaultUrl,
        ScalarTheme theme = ScalarTheme.Purple,
        string? basePath = null)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        
            var featureManager = app.Services.GetRequiredService<IFeatureManager>();
            if (await featureManager.IsEnabledAsync("ScalarUI"))
            {
                // Map Scalar for direct access (no basePath)
                var normalizedUrl = url.TrimStart('/');
                app.MapScalarApiReference($"/{normalizedUrl}", options =>
                {
                    options.WithTitle(title)
                           .WithTheme(theme)
                           .WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.HttpClient)
                           .WithTestRequestButton(true);
                });

                // Map Scalar for Gateway access if basePath is provided
                if (!string.IsNullOrWhiteSpace(basePath))
                {
                    var normalizedBasePath = basePath.Trim('/');
                    app.MapScalarApiReference($"/{normalizedBasePath}/{normalizedUrl}", options =>
                    {
                        options.WithTitle(title)
                               .WithTheme(theme)
                               .WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.HttpClient)
                               .WithTestRequestButton(true)
                               .WithOpenApiRoutePattern($"/{normalizedBasePath}/openapi/{{documentName}}.json");
                    });
                }
            }
        }
        return app;
    }
}
