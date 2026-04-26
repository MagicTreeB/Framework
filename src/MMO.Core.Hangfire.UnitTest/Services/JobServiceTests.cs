using System.Linq.Expressions;
using FluentAssertions;
using Hangfire;
using MMO.Core.Hangfire.Interfaces;
using MMO.Core.Hangfire.Services;
using Moq;
using Xunit;

namespace MMO.Core.Hangfire.UnitTest.Services;

public class JobServiceTests
{
    private readonly Mock<IBackgroundJobClient> _mockBackgroundJobClient;
    private readonly IJobService _jobService;

    public JobServiceTests()
    {
        _mockBackgroundJobClient = new Mock<IBackgroundJobClient>();
        _jobService = new JobService(_mockBackgroundJobClient.Object);
    }

    #region Enqueue Tests

    [Fact]
    public void Enqueue_WithSyncMethod_ShouldCallBackgroundJobClient()
    {
        // Arrange
        Expression<Action<TestService>> methodCall = x => x.DoWork();
        const string expectedJobId = "job-123";
        _mockBackgroundJobClient.Setup(x => x.Enqueue(methodCall)).Returns(expectedJobId);

        // Act
        var result = _jobService.Enqueue(methodCall);

        // Assert
        result.Should().Be(expectedJobId);
        _mockBackgroundJobClient.Verify(x => x.Enqueue(methodCall), Times.Once);
    }

    [Fact]
    public void Enqueue_WithAsyncMethod_ShouldCallBackgroundJobClient()
    {
        // Arrange
        Expression<Func<TestService, Task>> methodCall = x => x.DoWorkAsync();
        const string expectedJobId = "job-456";
        _mockBackgroundJobClient.Setup(x => x.Enqueue(methodCall)).Returns(expectedJobId);

        // Act
        var result = _jobService.Enqueue(methodCall);

        // Assert
        result.Should().Be(expectedJobId);
        _mockBackgroundJobClient.Verify(x => x.Enqueue(methodCall), Times.Once);
    }

    #endregion

    #region Schedule Tests

    [Fact]
    public void Schedule_WithSyncMethodAndTimeSpan_ShouldCallBackgroundJobClient()
    {
        // Arrange
        Expression<Action<TestService>> methodCall = x => x.DoWork();
        var delay = TimeSpan.FromMinutes(30);
        const string expectedJobId = "scheduled-123";
        _mockBackgroundJobClient.Setup(x => x.Schedule(methodCall, delay)).Returns(expectedJobId);

        // Act
        var result = _jobService.Schedule(methodCall, delay);

        // Assert
        result.Should().Be(expectedJobId);
        _mockBackgroundJobClient.Verify(x => x.Schedule(methodCall, delay), Times.Once);
    }

    [Fact]
    public void Schedule_WithAsyncMethodAndTimeSpan_ShouldCallBackgroundJobClient()
    {
        // Arrange
        Expression<Func<TestService, Task>> methodCall = x => x.DoWorkAsync();
        var delay = TimeSpan.FromHours(2);
        const string expectedJobId = "scheduled-456";
        _mockBackgroundJobClient.Setup(x => x.Schedule(methodCall, delay)).Returns(expectedJobId);

        // Act
        var result = _jobService.Schedule(methodCall, delay);

        // Assert
        result.Should().Be(expectedJobId);
        _mockBackgroundJobClient.Verify(x => x.Schedule(methodCall, delay), Times.Once);
    }

    [Fact]
    public void Schedule_WithSyncMethodAndDateTimeOffset_ShouldCallBackgroundJobClient()
    {
        // Arrange
        Expression<Action<TestService>> methodCall = x => x.DoWork();
        var enqueueAt = DateTimeOffset.UtcNow.AddDays(1);
        const string expectedJobId = "scheduled-789";
        _mockBackgroundJobClient.Setup(x => x.Schedule(methodCall, enqueueAt)).Returns(expectedJobId);

        // Act
        var result = _jobService.Schedule(methodCall, enqueueAt);

        // Assert
        result.Should().Be(expectedJobId);
        _mockBackgroundJobClient.Verify(x => x.Schedule(methodCall, enqueueAt), Times.Once);
    }

    [Fact]
    public void Schedule_WithAsyncMethodAndDateTimeOffset_ShouldCallBackgroundJobClient()
    {
        // Arrange
        Expression<Func<TestService, Task>> methodCall = x => x.DoWorkAsync();
        var enqueueAt = DateTimeOffset.UtcNow.AddHours(12);
        const string expectedJobId = "scheduled-101";
        _mockBackgroundJobClient.Setup(x => x.Schedule(methodCall, enqueueAt)).Returns(expectedJobId);

        // Act
        var result = _jobService.Schedule(methodCall, enqueueAt);

        // Assert
        result.Should().Be(expectedJobId);
        _mockBackgroundJobClient.Verify(x => x.Schedule(methodCall, enqueueAt), Times.Once);
    }

    #endregion

    #region Recurring Job Tests

    [Fact]
    public void AddOrUpdateRecurringJob_WithSyncMethod_ShouldNotThrow()
    {
        // Arrange
        const string jobId = "daily-cleanup";
        Expression<Action<TestService>> methodCall = x => x.DoWork();
        const string cronExpression = "0 0 * * *"; // Daily at midnight

        // Act
        Action act = () => _jobService.AddOrUpdateRecurringJob(jobId, methodCall, cronExpression);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddOrUpdateRecurringJob_WithAsyncMethod_ShouldNotThrow()
    {
        // Arrange
        const string jobId = "hourly-sync";
        Expression<Func<TestService, Task>> methodCall = x => x.DoWorkAsync();
        const string cronExpression = "0 * * * *"; // Every hour

        // Act
        Action act = () => _jobService.AddOrUpdateRecurringJob(jobId, methodCall, cronExpression);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RemoveRecurringJob_ShouldNotThrow()
    {
        // Arrange
        const string jobId = "job-to-remove";

        // Act
        Action act = () => _jobService.RemoveRecurringJob(jobId);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Delete and Requeue Tests

    [Fact]
    public void DeleteJob_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        const string jobId = "job-to-delete";
        _mockBackgroundJobClient.Setup(x => x.Delete(jobId)).Returns(true);

        // Act
        var result = _jobService.DeleteJob(jobId);

        // Assert
        result.Should().BeTrue();
        _mockBackgroundJobClient.Verify(x => x.Delete(jobId), Times.Once);
    }

    [Fact]
    public void DeleteJob_WhenFails_ShouldReturnFalse()
    {
        // Arrange
        const string jobId = "non-existent-job";
        _mockBackgroundJobClient.Setup(x => x.Delete(jobId)).Returns(false);

        // Act
        var result = _jobService.DeleteJob(jobId);

        // Assert
        result.Should().BeFalse();
        _mockBackgroundJobClient.Verify(x => x.Delete(jobId), Times.Once);
    }

    [Fact]
    public void RequeueJob_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        const string jobId = "failed-job";
        _mockBackgroundJobClient.Setup(x => x.Requeue(jobId)).Returns(true);

        // Act
        var result = _jobService.RequeueJob(jobId);

        // Assert
        result.Should().BeTrue();
        _mockBackgroundJobClient.Verify(x => x.Requeue(jobId), Times.Once);
    }

    [Fact]
    public void RequeueJob_WhenFails_ShouldReturnFalse()
    {
        // Arrange
        const string jobId = "non-existent-job";
        _mockBackgroundJobClient.Setup(x => x.Requeue(jobId)).Returns(false);

        // Act
        var result = _jobService.RequeueJob(jobId);

        // Assert
        result.Should().BeFalse();
        _mockBackgroundJobClient.Verify(x => x.Requeue(jobId), Times.Once);
    }

    #endregion

    #region Continuation Tests

    [Fact]
    public void ContinueWith_WithSyncMethod_ShouldCallBackgroundJobClient()
    {
        // Arrange
        const string parentJobId = "parent-123";
        Expression<Action<TestService>> methodCall = x => x.DoWork();
        const string expectedJobId = "continuation-123";
        _mockBackgroundJobClient.Setup(x => x.ContinueJobWith(parentJobId, methodCall)).Returns(expectedJobId);

        // Act
        var result = _jobService.ContinueWith(parentJobId, methodCall);

        // Assert
        result.Should().Be(expectedJobId);
        _mockBackgroundJobClient.Verify(x => x.ContinueJobWith(parentJobId, methodCall), Times.Once);
    }

    [Fact]
    public void ContinueWith_WithAsyncMethod_ShouldCallBackgroundJobClient()
    {
        // Arrange
        const string parentJobId = "parent-456";
        Expression<Func<TestService, Task>> methodCall = x => x.DoWorkAsync();
        const string expectedJobId = "continuation-456";
        _mockBackgroundJobClient.Setup(x => x.ContinueJobWith(parentJobId, methodCall)).Returns(expectedJobId);

        // Act
        var result = _jobService.ContinueWith(parentJobId, methodCall);

        // Assert
        result.Should().Be(expectedJobId);
        _mockBackgroundJobClient.Verify(x => x.ContinueJobWith(parentJobId, methodCall), Times.Once);
    }

    #endregion

    #region Helper Classes

    // Test service for expression building
    public class TestService
    {
        public void DoWork() { }
        public Task DoWorkAsync() => Task.CompletedTask;
    }

    #endregion
}
