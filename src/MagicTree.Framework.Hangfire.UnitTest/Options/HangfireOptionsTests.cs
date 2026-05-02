using FluentAssertions;
using MagicTree.Framework.Hangfire.Options;
using Xunit;

namespace MagicTree.Framework.Hangfire.UnitTest.Options;

public class HangfireOptionsTests
{
    [Fact]
    public void SectionName_ShouldReturnHangfire()
    {
        // Act
        var sectionName = HangfireOptions.SectionName;

        // Assert
        sectionName.Should().Be("Hangfire");
    }

    [Fact]
    public void HangfireOptions_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var options = new HangfireOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.StorageType.Should().Be("SqlServer");
        options.ConnectionString.Should().BeNull();
        options.Dashboard.Should().NotBeNull();
        options.Worker.Should().NotBeNull();
        options.Retry.Should().NotBeNull();
    }

    [Fact]
    public void HangfireOptions_AllProperties_ShouldBeSettable()
    {
        // Arrange
        var options = new HangfireOptions();

        // Act
        options.Enabled = false;
        options.StorageType = "Custom";
        options.ConnectionString = "Server=test;";
        options.Dashboard = new DashboardOptions();
        options.Worker = new WorkerOptions();
        options.Retry = new RetryOptions();

        // Assert
        options.Enabled.Should().BeFalse();
        options.StorageType.Should().Be("Custom");
        options.ConnectionString.Should().Be("Server=test;");
        options.Dashboard.Should().NotBeNull();
        options.Worker.Should().NotBeNull();
        options.Retry.Should().NotBeNull();
    }

    [Fact]
    public void DashboardOptions_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var options = new DashboardOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.Route.Should().Be("/hangfire");
        options.RequireAuthentication.Should().BeTrue();
        options.RequiredRole.Should().Be("Admin");
        options.Title.Should().Be("Hangfire Dashboard");
    }

    [Fact]
    public void DashboardOptions_AllProperties_ShouldBeSettable()
    {
        // Arrange
        var options = new DashboardOptions();

        // Act
        options.Enabled = false;
        options.Route = "/custom-dashboard";
        options.RequireAuthentication = false;
        options.RequiredRole = "Manager";
        options.Title = "Custom Dashboard";

        // Assert
        options.Enabled.Should().BeFalse();
        options.Route.Should().Be("/custom-dashboard");
        options.RequireAuthentication.Should().BeFalse();
        options.RequiredRole.Should().Be("Manager");
        options.Title.Should().Be("Custom Dashboard");
    }

    [Fact]
    public void WorkerOptions_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var options = new WorkerOptions();

        // Assert
        options.WorkerCount.Should().Be(20);
        options.Queues.Should().BeEquivalentTo(new[] { "default" });
        options.PollingIntervalSeconds.Should().Be(15);
    }

    [Fact]
    public void WorkerOptions_AllProperties_ShouldBeSettable()
    {
        // Arrange
        var options = new WorkerOptions();

        // Act
        options.WorkerCount = 50;
        options.Queues = new[] { "critical", "high", "normal", "low" };
        options.PollingIntervalSeconds = 30;

        // Assert
        options.WorkerCount.Should().Be(50);
        options.Queues.Should().BeEquivalentTo(new[] { "critical", "high", "normal", "low" });
        options.PollingIntervalSeconds.Should().Be(30);
    }

    [Fact]
    public void WorkerOptions_Queues_ShouldSupportMultipleValues()
    {
        // Arrange
        var options = new WorkerOptions
        {
            Queues = new[] { "queue1", "queue2", "queue3", "queue4", "queue5" }
        };

        // Assert
        options.Queues.Should().HaveCount(5);
        options.Queues.Should().Contain("queue1");
        options.Queues.Should().Contain("queue5");
    }

    [Fact]
    public void RetryOptions_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var options = new RetryOptions();

        // Assert
        options.MaxRetryAttempts.Should().Be(10);
        options.UseExponentialBackoff.Should().BeTrue();
        options.InitialDelaySeconds.Should().Be(60);
    }

    [Fact]
    public void RetryOptions_AllProperties_ShouldBeSettable()
    {
        // Arrange
        var options = new RetryOptions();

        // Act
        options.MaxRetryAttempts = 5;
        options.UseExponentialBackoff = false;
        options.InitialDelaySeconds = 30;

        // Assert
        options.MaxRetryAttempts.Should().Be(5);
        options.UseExponentialBackoff.Should().BeFalse();
        options.InitialDelaySeconds.Should().Be(30);
    }

    [Fact]
    public void HangfireOptions_ComplexConfiguration_ShouldBeSettable()
    {
        // Arrange & Act
        var options = new HangfireOptions
        {
            Enabled = true,
            StorageType = "SqlServer",
            ConnectionString = "Server=localhost;Database=HangfireProduction;Trusted_Connection=true;",
            Dashboard = new DashboardOptions
            {
                Enabled = true,
                Route = "/jobs",
                RequireAuthentication = true,
                RequiredRole = "SuperAdmin",
                Title = "Production Job Dashboard"
            },
            Worker = new WorkerOptions
            {
                WorkerCount = 30,
                Queues = new[] { "critical", "high", "default", "low" },
                PollingIntervalSeconds = 10
            },
            Retry = new RetryOptions
            {
                MaxRetryAttempts = 15,
                UseExponentialBackoff = true,
                InitialDelaySeconds = 120
            }
        };

        // Assert
        options.Enabled.Should().BeTrue();
        options.StorageType.Should().Be("SqlServer");
        options.ConnectionString.Should().Contain("HangfireProduction");
        
        options.Dashboard.Route.Should().Be("/jobs");
        options.Dashboard.RequiredRole.Should().Be("SuperAdmin");
        options.Dashboard.Title.Should().Be("Production Job Dashboard");
        
        options.Worker.WorkerCount.Should().Be(30);
        options.Worker.Queues.Should().HaveCount(4);
        options.Worker.PollingIntervalSeconds.Should().Be(10);
        
        options.Retry.MaxRetryAttempts.Should().Be(15);
        options.Retry.InitialDelaySeconds.Should().Be(120);
    }

    [Fact]
    public void WorkerOptions_EmptyQueues_ShouldBeAllowed()
    {
        // Arrange & Act
        var options = new WorkerOptions
        {
            Queues = Array.Empty<string>()
        };

        // Assert
        options.Queues.Should().BeEmpty();
    }

    [Fact]
    public void DashboardOptions_EmptyRequiredRole_ShouldBeAllowed()
    {
        // Arrange & Act
        var options = new DashboardOptions
        {
            RequiredRole = string.Empty
        };

        // Assert
        options.RequiredRole.Should().BeEmpty();
    }

    [Fact]
    public void RetryOptions_ZeroRetryAttempts_ShouldBeAllowed()
    {
        // Arrange & Act
        var options = new RetryOptions
        {
            MaxRetryAttempts = 0
        };

        // Assert
        options.MaxRetryAttempts.Should().Be(0);
    }
}
