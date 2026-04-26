using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MMO.Core.Hangfire.Extensions;
using MMO.Core.Hangfire.Interfaces;
using MMO.Core.Hangfire.Options;
using Xunit;

namespace MMO.Core.Hangfire.UnitTest.Extensions;

public class HangfireExtensionsTests
{
    [Fact]
    public void AddHangfireServices_WithValidSqlServerConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:Enabled"] = "true",
                ["Hangfire:StorageType"] = "SqlServer",
                ["Hangfire:ConnectionString"] = "Server=localhost;Database=HangfireTest;Trusted_Connection=true;",
                ["Hangfire:Dashboard:Enabled"] = "true",
                ["Hangfire:Dashboard:Route"] = "/hangfire",
                ["Hangfire:Worker:WorkerCount"] = "10",
                ["Hangfire:Worker:Queues:0"] = "default",
                ["Hangfire:Worker:Queues:1"] = "priority"
            })
            .Build();

        // Act
        services.AddHangfireServices(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var jobService = serviceProvider.GetService<IJobService>();
        jobService.Should().NotBeNull();
    }

    [Fact]
    public void AddHangfireServices_WhenDisabled_ShouldNotRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:Enabled"] = "false"
            })
            .Build();

        // Act
        services.AddHangfireServices(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var jobService = serviceProvider.GetService<IJobService>();
        jobService.Should().BeNull();
    }

    [Fact]
    public void AddHangfireServices_WithMissingConnectionString_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:Enabled"] = "true",
                ["Hangfire:StorageType"] = "SqlServer"
                // Missing ConnectionString
            })
            .Build();

        // Act
        Action act = () => services.AddHangfireServices(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionString is required*");
    }

    [Fact]
    public void AddHangfireServices_WithUnsupportedStorageType_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:Enabled"] = "true",
                ["Hangfire:StorageType"] = "InMemory", // Not supported
                ["Hangfire:ConnectionString"] = "dummy"
            })
            .Build();

        // Act
        Action act = () => services.AddHangfireServices(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unsupported Hangfire StorageType*");
    }

    [Fact]
    public void AddHangfireServices_ShouldBindOptionsFromConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:Enabled"] = "true",
                ["Hangfire:StorageType"] = "SqlServer",
                ["Hangfire:ConnectionString"] = "Server=localhost;Database=HangfireTest;",
                ["Hangfire:Dashboard:Title"] = "Custom Dashboard",
                ["Hangfire:Worker:WorkerCount"] = "25"
            })
            .Build();

        // Act
        services.AddHangfireServices(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<HangfireOptions>>();
        options.Should().NotBeNull();
        options!.Value.Enabled.Should().BeTrue();
        options.Value.StorageType.Should().Be("SqlServer");
        options.Value.Dashboard.Title.Should().Be("Custom Dashboard");
        options.Value.Worker.WorkerCount.Should().Be(25);
    }

    [Fact]
    public void AddHangfireServices_ShouldReturnSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:Enabled"] = "true",
                ["Hangfire:StorageType"] = "SqlServer",
                ["Hangfire:ConnectionString"] = "Server=localhost;Database=Test;"
            })
            .Build();

        // Act
        var result = services.AddHangfireServices(configuration);

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddHangfireServices_ShouldRegisterJobServiceAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:Enabled"] = "true",
                ["Hangfire:StorageType"] = "SqlServer",
                ["Hangfire:ConnectionString"] = "Server=localhost;Database=Test;"
            })
            .Build();

        // Act
        services.AddHangfireServices(configuration);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IJobService));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddHangfireServices_WithCustomWorkerOptions_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:Enabled"] = "true",
                ["Hangfire:StorageType"] = "SqlServer",
                ["Hangfire:ConnectionString"] = "Server=localhost;Database=Test;",
                ["Hangfire:Worker:WorkerCount"] = "50",
                ["Hangfire:Worker:PollingIntervalSeconds"] = "30",
                ["Hangfire:Worker:Queues:0"] = "critical",
                ["Hangfire:Worker:Queues:1"] = "default",
                ["Hangfire:Worker:Queues:2"] = "low"
            })
            .Build();

        // Act
        services.AddHangfireServices(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<HangfireOptions>>();
        options!.Value.Worker.WorkerCount.Should().Be(50);
        options.Value.Worker.PollingIntervalSeconds.Should().Be(30);
        options.Value.Worker.Queues.Should().BeEquivalentTo(new[] { "critical", "default", "low" });
    }

    [Fact]
    public void AddHangfireServices_WithEmptyConfiguration_ShouldUseDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:Enabled"] = "true",
                ["Hangfire:StorageType"] = "SqlServer",
                ["Hangfire:ConnectionString"] = "Server=localhost;Database=Test;"
            })
            .Build();

        // Act
        services.AddHangfireServices(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<HangfireOptions>>();
        options!.Value.Dashboard.Route.Should().Be("/hangfire");
        options.Value.Dashboard.Title.Should().Be("Hangfire Dashboard");
        options.Value.Worker.WorkerCount.Should().Be(20);
        options.Value.Worker.PollingIntervalSeconds.Should().Be(15);
    }

    [Fact]
    public void AddHangfireServices_ShouldAllowMethodChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hangfire:Enabled"] = "true",
                ["Hangfire:StorageType"] = "SqlServer",
                ["Hangfire:ConnectionString"] = "Server=localhost;Database=Test;"
            })
            .Build();

        // Act
        var result = services
            .AddHangfireServices(configuration)
            .AddSingleton<IConfiguration>(configuration);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(services);
    }

    // Note: UseHangfireDashboardWithAuth requires AspNetCore pipeline which is difficult to unit test
    // It's better tested through integration tests with WebApplicationFactory
    // For now, we focus on service registration tests
}
