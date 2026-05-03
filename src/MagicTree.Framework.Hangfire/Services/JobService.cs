using System.Linq.Expressions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using MagicTree.Framework.Hangfire.Interfaces;

namespace MagicTree.Framework.Hangfire.Services;

/// <summary>
/// Service implementation for managing background jobs using Hangfire
/// </summary>
public class JobService : IJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;

    public JobService(IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager)
    {
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
    }

    /// <inheritdoc />
    public string Enqueue<T>(Expression<Action<T>> methodCall)
    {
        return _backgroundJobClient.Create(Job.FromExpression(methodCall), new EnqueuedState());
    }

    /// <inheritdoc />
    public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
    {
        return _backgroundJobClient.Create(Job.FromExpression(methodCall), new EnqueuedState());
    }

    /// <inheritdoc />
    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay)
    {
        return _backgroundJobClient.Create(Job.FromExpression(methodCall), new ScheduledState(delay));
    }

    /// <inheritdoc />
    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
    {
        return _backgroundJobClient.Create(Job.FromExpression(methodCall), new ScheduledState(delay));
    }

    /// <inheritdoc />
    public string Schedule<T>(Expression<Action<T>> methodCall, DateTimeOffset enqueueAt)
    {
        return _backgroundJobClient.Create(Job.FromExpression(methodCall), new ScheduledState(enqueueAt.UtcDateTime));
    }

    /// <inheritdoc />
    public string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt)
    {
        return _backgroundJobClient.Create(Job.FromExpression(methodCall), new ScheduledState(enqueueAt.UtcDateTime));
    }

    /// <inheritdoc />
    public void AddOrUpdateRecurringJob<T>(string jobId, Expression<Action<T>> methodCall, string cronExpression)
    {
        _recurringJobManager.AddOrUpdate(jobId, Job.FromExpression(methodCall), cronExpression);
    }

    /// <inheritdoc />
    public void AddOrUpdateRecurringJob<T>(string jobId, Expression<Func<T, Task>> methodCall, string cronExpression)
    {
        _recurringJobManager.AddOrUpdate(jobId, Job.FromExpression(methodCall), cronExpression);
    }

    /// <inheritdoc />
    public void RemoveRecurringJob(string jobId)
    {
        _recurringJobManager.RemoveIfExists(jobId);
    }

    /// <inheritdoc />
    public bool DeleteJob(string jobId)
    {
        return _backgroundJobClient.ChangeState(jobId, new DeletedState(), null!);
    }

    /// <inheritdoc />
    public bool RequeueJob(string jobId)
    {
        return _backgroundJobClient.ChangeState(jobId, new EnqueuedState(), FailedState.StateName);
    }

    /// <inheritdoc />
    public string ContinueWith<T>(string parentJobId, Expression<Action<T>> methodCall)
    {
        return _backgroundJobClient.Create(Job.FromExpression(methodCall), new AwaitingState(parentJobId));
    }

    /// <inheritdoc />
    public string ContinueWith<T>(string parentJobId, Expression<Func<T, Task>> methodCall)
    {
        return _backgroundJobClient.Create(Job.FromExpression(methodCall), new AwaitingState(parentJobId));
    }
}
