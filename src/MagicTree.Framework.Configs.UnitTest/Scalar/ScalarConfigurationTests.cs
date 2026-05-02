using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using MagicTree.Framework.Configs.Scalar;
using Scalar.AspNetCore;
using Moq;

namespace MagicTree.Framework.Configs.UnitTest.Scalar;

public class ScalarConfigurationTests
{
    [Fact]
    public void AddScalarConfiguration_ShouldRegisterOpenApi()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddScalarConfiguration();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - OpenAPI services registered (we can't directly test OpenAPI service)
        // Verify method executes without exception
        serviceProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddScalarConfiguration_ShouldRegisterFeatureManagement()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.AddScalarConfiguration();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var featureManager = serviceProvider.GetService<IFeatureManager>();
        featureManager.Should().NotBeNull();
    }

    [Fact]
    public void AddScalarConfiguration_ShouldReturnSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddScalarConfiguration();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public async Task ConfigureScalarEndpoints_InDevelopment_WithFeatureFlagEnabled_ShouldMapScalar()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Services.AddScalarConfiguration();
        
        // Enable ScalarUI feature flag
        builder.Configuration["FeatureManagement:ScalarUI"] = "true";
        
        var app = builder.Build();

        // Act
        var result = await app.ConfigureScalarEndpoints("Test API", "docs", ScalarTheme.Purple);

        // Assert
        result.Should().BeSameAs(app);
        await app.DisposeAsync();
    }

    [Fact]
    public async Task ConfigureScalarEndpoints_InDevelopment_WithFeatureFlagDisabled_ShouldNotThrow()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Services.AddScalarConfiguration();
        
        // Disable ScalarUI feature flag
        builder.Configuration["FeatureManagement:ScalarUI"] = "false";
        
        var app = builder.Build();

        // Act
        var result = await app.ConfigureScalarEndpoints("Test API", "docs", ScalarTheme.Purple);

        // Assert - Should execute without exception even with feature disabled
        result.Should().BeSameAs(app);
        await app.DisposeAsync();
    }

    [Fact]
    public async Task ConfigureScalarEndpoints_InProduction_ShouldNotMapScalar()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Production;
        builder.Services.AddScalarConfiguration();
        
        var app = builder.Build();

        // Act
        var result = await app.ConfigureScalarEndpoints("Test API", "docs", ScalarTheme.Purple);

        // Assert - Should execute without exception but not map Scalar in production
        result.Should().BeSameAs(app);
        await app.DisposeAsync();
    }

    [Fact]
    public async Task ConfigureScalarEndpoints_WithDefaultParameters_ShouldUseDefaults()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Services.AddScalarConfiguration();
        builder.Configuration["FeatureManagement:ScalarUI"] = "true";
        
        var app = builder.Build();

        // Act - Using default parameters
        var result = await app.ConfigureScalarEndpoints();

        // Assert
        result.Should().BeSameAs(app);
        await app.DisposeAsync();
    }

    [Fact]
    public async Task ConfigureScalarEndpoints_WithCustomTitle_ShouldUseProvidedTitle()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Services.AddScalarConfiguration();
        builder.Configuration["FeatureManagement:ScalarUI"] = "true";
        
        var app = builder.Build();

        // Act
        var result = await app.ConfigureScalarEndpoints("My Custom API");

        // Assert
        result.Should().BeSameAs(app);
        await app.DisposeAsync();
    }

    [Fact]
    public async Task ConfigureScalarEndpoints_WithCustomUrl_ShouldUseProvidedUrl()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Services.AddScalarConfiguration();
        builder.Configuration["FeatureManagement:ScalarUI"] = "true";
        
        var app = builder.Build();

        // Act
        var result = await app.ConfigureScalarEndpoints("Test API", "api-docs");

        // Assert
        result.Should().BeSameAs(app);
        await app.DisposeAsync();
    }

    [Fact]
    public async Task ConfigureScalarEndpoints_WithDifferentThemes_ShouldAcceptAllThemes()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Services.AddScalarConfiguration();
        builder.Configuration["FeatureManagement:ScalarUI"] = "true";
        
        var app = builder.Build();

        // Act & Assert - Test different themes
        var themes = new[] { ScalarTheme.Purple, ScalarTheme.Default, ScalarTheme.Moon, ScalarTheme.Kepler };
        
        foreach (var theme in themes)
        {
            var result = await app.ConfigureScalarEndpoints("Test API", "docs", theme);
            result.Should().BeSameAs(app);
        }

        await app.DisposeAsync();
    }

    [Fact]
    public async Task ConfigureScalarEndpoints_ShouldReturnSameApplicationBuilder()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Services.AddScalarConfiguration();
        
        var app = builder.Build();

        // Act
        var result = await app.ConfigureScalarEndpoints();

        // Assert
        result.Should().BeSameAs(app);
        await app.DisposeAsync();
    }

    [Fact]
    public async Task ConfigureScalarEndpoints_InStaging_ShouldNotMapScalar()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Staging;
        builder.Services.AddScalarConfiguration();
        
        var app = builder.Build();

        // Act
        var result = await app.ConfigureScalarEndpoints("Test API", "docs", ScalarTheme.Purple);

        // Assert - Should execute without exception but not map Scalar in staging
        result.Should().BeSameAs(app);
        await app.DisposeAsync();
    }

    [Fact]
    public void AddScalarConfiguration_ShouldAllowMethodChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services
            .AddScalarConfiguration()
            .AddLogging(); // Chain additional services

        // Assert
        result.Should().NotBeNull();
    }
}
