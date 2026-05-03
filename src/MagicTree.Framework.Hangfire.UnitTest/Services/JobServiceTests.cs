using System.Linq.Expressions;
using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using MagicTree.Framework.Hangfire.Interfaces;
using MagicTree.Framework.Hangfire.Services;
using Moq;
using Xunit;

namespace MagicTree.Framework.Hangfire.UnitTest.Services;

public class JobServiceTests
{
    private readonly Mock<IBackgroundJobClient> _mockBackgroundJobClient;
    private readonly Mock<IRecurringJobManager> _mockRecurringJobManager;
    private readonly IJobService _jobService;

    public JobServiceTests()
    {
        _mockBackgroundJobClient = new Mock<IBackgroundJobClient>();
        _mockRecurringJobManager = new Mock<IRecurringJobManager>();
        _jobService = new JobService(_mockBackgroundJobClient.Object, _mockRecurringJobManager.Object);
    }

    #region Enqueue Tests

    [Fact]
    public void Enqueue_WithSyncMethod_ShouldCallBackgroundJobClient()
    {
        // Arrange
        Expression<Action<TestService>> methodCall = x => x.DoWork();
        const string expectedJobId = "job-123";
        _mockBackgroundJobClient.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns(expectedJobId);

        // Act
        var result = _jobService.Enqueue(methodCall);

        // Assert
        result.Should().Be(expectedJobId);
        _mockBackgroundJobClient.Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }

    [Fact]
    public void Enqueue_WithAsyncMethod_ShouldCallBackgroundJobClient()
    {
        // Arrange
        Expression<Func<TestService, Task>> methodCall = x => x.DoWorkAsync();
        const string expectedJobId = "job-456";
        _mockBackgroundJobClient.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns(expectedJobId);

        // Act
        var result = _jobService.Enqueue(methodCall);

        // Assert
        result.Should().Be(expectedJobId);
        _mockBackgroundJobClient.Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
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
        _mockBackgroundJobClient.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns(expectedJobId);

        // Act
        var result = _jobService.Schedule(methodCall, delay);

        // Assert
        result.Should().Be(expectedJobId);
        _mockBackgroundJobClient.Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }

    [Fact]
    public void Schedule_WithAsyncMethodAndTimeSpan_ShouldCallBackgroundJobClient()
    {
        // Arrange
        Expression<Func<TestService, Task>> methodCall = x => x.DoWorkAsync();
        var delay = TimeSpan.FromHours(2);
        const string expectedJobId = "scheduled-456";
        _mockBackgroundJobClient.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns(expectedJobId);

        // Act
        var result = _jobService.Schedule(methodCall, delay);

        // Assert
        result.Should().Be(expectedJobId);
        _mockBackgroundJobClient.Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }

    [Fact]
    public void Schedule_WithSyncMethodAndDateTimeOffset_ShouldCallBackgroundJobClient()
    {
        // Arrange
        Expression<Action<TestService>> methodCall = x => x.DoWork();
        var enqueueAt = DateTimeOffset.UtcNow.AddDays(1);
        const string expectedJobId = "scheduled-789";
        _mockBackgroundJobClient.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns(expectedJobId);

        // Act
        var result = _jobService.Schedule(methodCall, enqueueAt);

        // Assert
        result.Should().Be(expectedJobId);
        _mockBackgroundJobClient.Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }

    [Fact]
    public void Schedule_WithAsyncMethodAndDateTimeOffset_ShouldCallBackgroundJobClient()
    {
        // Arrange
        Expression<Func<TestService, Task>> methodCall = x => x.DoWorkAsync();
        var enqueueAt = DateTimeOffset.UtcNow.AddHours(12);
        const string expectedJobId = "scheduled-101";
        _mockBackgroundJobClient.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns(expectedJobId);

        // Act
        var result = _jobService.Schedule(methodCall, enqueueAt);

        // Assert
        result.Should().Be(expectedJobId);
        _mockBackgroundJobClient.Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }

    #endregion

    #region Recurring Job Tests

    [Fact]
    public void AddOrUpdateRecurringJob_WithSyncMethod_ShouldNotThrow()
    {
        // Arrange
        const string jobId = "daily-cleanup";
        Expression<Action<TestService>> methodCall = x => x.DoWork();
        const string cronExpression = "0 0 * * *";

        // Act
        Action act = () => _jobService.AddOrUpdateRecurringJob(jobId, methodCall, cronExpression);

        // Assert
        act.Should().NotThrow();
        _mockRecurringJobManager.Verify(x => x.AddOrUpdate(jobId, It.IsAny<Job>(), cronExpression, It.IsAny<RecurringJobOptions>()), Times.Once);
    }

    [Fact]
    public void AddOrUpdateRecurringJob_WithAsyncMethod_ShouldNotThrow()
    {
        // Arrange
        const string jobId = "hourly-sync";
        Expression<Func<TestService, Task>> methodCall = x => x.DoWorkAsync();
        const string cronExpression = "0 * * * *";

        // Act
        Action act = () => _jobService.AddOrUpdateRecurringJob(jobId, methodCall, cronExpression);

        // Assert
        act.Should().NotThrow();
        _mockRecurringJobManager.Verify(x => x.AddOrUpdate(jobId, It.IsAny<Job>(), cronExpression, It.IsAny<RecurringJobOptions>()), Times.Once);
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
        _mockRecurringJobManager.Verify(x => x.RemoveIfExists(jobId), Times.Once);
    }

    #endregion

    #region Delete and Requeue Tests

    [Fact]
    public void DeleteJob_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        const string jobId = "job-to-delete";
        _mockBackgroundJobClient.Setup(x => x.ChangeState(jobId, It.IsAny<IState>(), It.IsAny<string>())).Returns(true);

        // Act
        var result = _jobService.DeleteJob(jobId);

        // Assert
        result.Should().BeTrue();
        _mockBackgroundJobClient.Verify(x => x.ChangeState(jobId, It.IsAny<IState>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void DeleteJob_WhenFails_ShouldReturnFalse()
    {
        // Arrange
        const string jobId = "non-existent-job";
        _mockBackgroundJobClient.Setup(x => x.ChangeState(jobId, It.IsAny<IState>(), It.IsAny<string>())).Returns(false);

        // Act
        var result = _jobService.DeleteJob(jobId);

        // Assert
        result.Should().BeFalse();
        _mockBackgroundJobClient.Verify(x => x.ChangeState(jobId, It.IsAny<IState>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void RequeueJob_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        const string jobId = "failed-job";
        _mockBackgroundJobClient.Setup(x => x.ChangeState(jobId, It.IsAny<IState>(), It.IsAny<string>())).Returns(true);

        // Act
        var result = _jobService.RequeueJob(jobId);

        // Assert
        result.Should().BeTrue();
        _mockBackgroundJobClient.Verify(x => x.ChangeState(jobId, It.IsAny<IState>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void RequeueJob_WhenFails_ShouldReturnFalse()
    {
        // Arrange
        const string jobId = "non-existent-job";
        _mockBackgroundJobClient.Setup(x => x.ChangeState(jobId, It.IsAny<IState>(), It.IsAny<string>())).Returns(false);

        // Act
        var result = _jobService.RequeueJob(jobId);

        // Assert
        result.Should().BeFalse();
        _mockBackgroundJobClient.Verify(x => x.ChangeState(jobId, It.IsAny<IState>(), It.IsAny<string>()), Times.Once);
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
        _mockBackgroundJobClient.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns(expectedJobId);

        // Act
        var result = _jobService.ContinueWith(parentJobId, methodCall);

        // Assert
        result.Should().Be(expectedJobId);
        _mockBackgroundJobClient.Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }

    [Fact]
    public void ContinueWith_WithAsyncMethod_ShouldCallBackgroundJobClient()
    {
        // Arrange
        const string parentJobId = "parent-456";
        Expression<Func<TestService, Task>> methodCall = x => x.DoWorkAsync();
        const string expectedJobId = "continuation-456";
        _mockBackgroundJobClient.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns(expectedJobId);

        // Act
        var result = _jobService.ContinueWith(parentJobId, methodCall);

        // Assert
        result.Should().Be(expectedJobId);
        _mockBackgroundJobClient.Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }

    #endregion

    #region Helper Classes

    public class TestService
    {
        public void DoWork() { }
        public Task DoWorkAsync() => Task.CompletedTask;
    }

    #endregion
}
