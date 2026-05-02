using System.Linq.Expressions;
using Hangfire;
using MagicTree.Framework.Hangfire.Interfaces;

namespace MagicTree.Framework.Hangfire.Services;

/// <summary>
/// Service implementation for managing background jobs using Hangfire
/// </summary>
public class JobService : IJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public JobService(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    /// <inheritdoc />
    public string Enqueue<T>(Expression<Action<T>> methodCall)
    {
        return _backgroundJobClient.Enqueue(methodCall);
    }

    /// <inheritdoc />
    public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
    {
        return _backgroundJobClient.Enqueue(methodCall);
    }

    /// <inheritdoc />
    public string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay)
    {
        return _backgroundJobClient.Schedule(methodCall, delay);
    }

    /// <inheritdoc />
    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
    {
        return _backgroundJobClient.Schedule(methodCall, delay);
    }

    /// <inheritdoc />
    public string Schedule<T>(Expression<Action<T>> methodCall, DateTimeOffset enqueueAt)
    {
        return _backgroundJobClient.Schedule(methodCall, enqueueAt);
    }

    /// <inheritdoc />
    public string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt)
    {
        return _backgroundJobClient.Schedule(methodCall, enqueueAt);
    }

    /// <inheritdoc />
    public void AddOrUpdateRecurringJob<T>(string jobId, Expression<Action<T>> methodCall, string cronExpression)
    {
        RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);
    }

    /// <inheritdoc />
    public void AddOrUpdateRecurringJob<T>(string jobId, Expression<Func<T, Task>> methodCall, string cronExpression)
    {
        RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);
    }

    /// <inheritdoc />
    public void RemoveRecurringJob(string jobId)
    {
        RecurringJob.RemoveIfExists(jobId);
    }

    /// <inheritdoc />
    public bool DeleteJob(string jobId)
    {
        return _backgroundJobClient.Delete(jobId);
    }

    /// <inheritdoc />
    public bool RequeueJob(string jobId)
    {
        return _backgroundJobClient.Requeue(jobId);
    }

    /// <inheritdoc />
    public string ContinueWith<T>(string parentJobId, Expression<Action<T>> methodCall)
    {
        return _backgroundJobClient.ContinueJobWith(parentJobId, methodCall);
    }

    /// <inheritdoc />
    public string ContinueWith<T>(string parentJobId, Expression<Func<T, Task>> methodCall)
    {
        return _backgroundJobClient.ContinueJobWith(parentJobId, methodCall);
    }
}
