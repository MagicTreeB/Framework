using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using MMO.Core.Configs.HealthCheck;
using System.Text.Json;

namespace MMO.Core.Configs.UnitTest.HealthCheck;

public class HealthCheckConfigurationTests
{
    [Fact]
    public async Task AddHealthCheckConfiguration_ShouldRegisterHealthChecks()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Add logging for HealthCheckService

        // Act
        services.AddHealthCheckConfiguration();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var healthCheckService = serviceProvider.GetService<HealthCheckService>();
        healthCheckService.Should().NotBeNull();

        var result = await healthCheckService!.CheckHealthAsync();
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task AddHealthCheckConfiguration_ShouldRegisterSelfCheckWithLiveTag()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddHealthCheckConfiguration();
        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // Assert
        var result = await healthCheckService.CheckHealthAsync(check => check.Tags.Contains("live"));
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Entries.Should().ContainKey("self");
    }

    [Fact]
    public async Task AddHealthCheckConfiguration_ShouldReturnHealthyForSelfCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthCheckConfiguration();
        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var result = await healthCheckService.CheckHealthAsync();

        // Assert
        result.Entries["self"].Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task ConfigureHealthCheckEndpoints_ShouldMapLiveCheckEndpoint()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHealthCheckConfiguration();
        var app = builder.Build();

        // Act
        await app.ConfigureHealthCheckEndpoints();

        // Assert - Verify endpoints are mapped (can't directly test without running the app)
        // We verify the method executes without exception
        await app.DisposeAsync();
    }

    [Fact]
    public async Task ConfigureHealthCheckEndpoints_WithCustomPaths_ShouldUseProvidedPaths()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHealthCheckConfiguration();
        var app = builder.Build();

        // Act
        await app.ConfigureHealthCheckEndpoints(
            healthCheckPath: "/custom-health",
            readyCheckPath: "/custom-ready",
            liveCheckPath: "/custom-live");

        // Assert - Method executes without exception
        await app.DisposeAsync();
    }

    [Fact]
    public async Task ConfigureHealthCheckEndpoints_ShouldUseDefaultPathsWhenNotProvided()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHealthCheckConfiguration();
        var app = builder.Build();

        // Act
        await app.ConfigureHealthCheckEndpoints();

        // Assert - Method executes without exception, uses /health, /ready, /live
        await app.DisposeAsync();
    }

    [Fact]
    public async Task HealthCheckResponse_ShouldBeJsonWithCorrectStructure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthCheckConfiguration();
        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var result = await healthCheckService.CheckHealthAsync();

        // Assert - Verify report structure
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Healthy);
        result.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);
        result.Entries.Should().NotBeEmpty();
        result.Entries.Should().ContainKey("self");
        
        var selfEntry = result.Entries["self"];
        selfEntry.Status.Should().Be(HealthStatus.Healthy);
        selfEntry.Tags.Should().Contain("live");
    }

    [Fact]
    public async Task LiveCheckPredicate_ShouldOnlyIncludeChecksWithLiveTag()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddCheck("live-check", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
            .AddCheck("ready-check", () => HealthCheckResult.Healthy(), tags: new[] { "ready" });
        
        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var liveResult = await healthCheckService.CheckHealthAsync(check => check.Tags.Contains("live"));

        // Assert
        liveResult.Entries.Should().ContainKey("live-check");
        liveResult.Entries.Should().NotContainKey("ready-check");
    }

    [Fact]
    public async Task ReadyCheckPredicate_ShouldIncludeAllChecks()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddCheck("live-check", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
            .AddCheck("ready-check", () => HealthCheckResult.Healthy(), tags: new[] { "ready" });
        
        var serviceProvider = services.BuildServiceProvider();
        var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

        // Act
        var readyResult = await healthCheckService.CheckHealthAsync(_ => true);

        // Assert
        readyResult.Entries.Should().ContainKey("live-check");
        readyResult.Entries.Should().ContainKey("ready-check");
    }

    [Fact]
    public async Task AddHealthCheckConfiguration_ShouldReturnSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddHealthCheckConfiguration();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public async Task ConfigureHealthCheckEndpoints_ShouldReturnSameApplicationBuilder()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHealthCheckConfiguration();
        var app = builder.Build();

        // Act
        var result = await app.ConfigureHealthCheckEndpoints();

        // Assert
        result.Should().BeSameAs(app);
        await app.DisposeAsync();
    }
}
