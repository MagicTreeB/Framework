using System.Linq.Expressions;

namespace MagicTree.Framework.Hangfire.Interfaces;

/// <summary>
/// Service interface for managing background jobs
/// </summary>
public interface IJobService
{
    /// <summary>
    /// Enqueue a fire-and-forget job to execute immediately
    /// </summary>
    /// <typeparam name="T">Service type containing the method to execute</typeparam>
    /// <param name="methodCall">Expression representing the method to call</param>
    /// <returns>Job ID for tracking</returns>
    string Enqueue<T>(Expression<Action<T>> methodCall);

    /// <summary>
    /// Enqueue a fire-and-forget job to execute immediately (async version)
    /// </summary>
    /// <typeparam name="T">Service type containing the method to execute</typeparam>
    /// <param name="methodCall">Expression representing the async method to call</param>
    /// <returns>Job ID for tracking</returns>
    string Enqueue<T>(Expression<Func<T, Task>> methodCall);

    /// <summary>
    /// Schedule a job to execute after a specified delay
    /// </summary>
    /// <typeparam name="T">Service type containing the method to execute</typeparam>
    /// <param name="methodCall">Expression representing the method to call</param>
    /// <param name="delay">Time to wait before execution</param>
    /// <returns>Job ID for tracking</returns>
    string Schedule<T>(Expression<Action<T>> methodCall, TimeSpan delay);

    /// <summary>
    /// Schedule a job to execute after a specified delay (async version)
    /// </summary>
    /// <typeparam name="T">Service type containing the method to execute</typeparam>
    /// <param name="methodCall">Expression representing the async method to call</param>
    /// <param name="delay">Time to wait before execution</param>
    /// <returns>Job ID for tracking</returns>
    string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay);

    /// <summary>
    /// Schedule a job to execute at a specific date/time
    /// </summary>
    /// <typeparam name="T">Service type containing the method to execute</typeparam>
    /// <param name="methodCall">Expression representing the method to call</param>
    /// <param name="enqueueAt">Exact time to execute the job</param>
    /// <returns>Job ID for tracking</returns>
    string Schedule<T>(Expression<Action<T>> methodCall, DateTimeOffset enqueueAt);

    /// <summary>
    /// Schedule a job to execute at a specific date/time (async version)
    /// </summary>
    /// <typeparam name="T">Service type containing the method to execute</typeparam>
    /// <param name="methodCall">Expression representing the async method to call</param>
    /// <param name="enqueueAt">Exact time to execute the job</param>
    /// <returns>Job ID for tracking</returns>
    string Schedule<T>(Expression<Func<T, Task>> methodCall, DateTimeOffset enqueueAt);

    /// <summary>
    /// Add or update a recurring job with cron expression
    /// </summary>
    /// <param name="jobId">Unique identifier for the recurring job</param>
    /// <param name="methodCall">Expression representing the method to call</param>
    /// <param name="cronExpression">Cron expression (e.g., "0 0 * * *" for daily at midnight)</param>
    void AddOrUpdateRecurringJob<T>(string jobId, Expression<Action<T>> methodCall, string cronExpression);

    /// <summary>
    /// Add or update a recurring job with cron expression (async version)
    /// </summary>
    /// <param name="jobId">Unique identifier for the recurring job</param>
    /// <param name="methodCall">Expression representing the async method to call</param>
    /// <param name="cronExpression">Cron expression (e.g., "0 0 * * *" for daily at midnight)</param>
    void AddOrUpdateRecurringJob<T>(string jobId, Expression<Func<T, Task>> methodCall, string cronExpression);

    /// <summary>
    /// Remove a recurring job by ID
    /// </summary>
    /// <param name="jobId">The recurring job ID to remove</param>
    void RemoveRecurringJob(string jobId);

    /// <summary>
    /// Delete a job by ID (scheduled, enqueued, or processing)
    /// </summary>
    /// <param name="jobId">The job ID to delete</param>
    /// <returns>True if deleted successfully</returns>
    bool DeleteJob(string jobId);

    /// <summary>
    /// Requeue a failed job by ID
    /// </summary>
    /// <param name="jobId">The failed job ID to requeue</param>
    /// <returns>True if requeued successfully</returns>
    bool RequeueJob(string jobId);

    /// <summary>
    /// Create a continuation job that executes after parent job completes
    /// </summary>
    /// <typeparam name="T">Service type containing the method to execute</typeparam>
    /// <param name="parentJobId">The parent job ID</param>
    /// <param name="methodCall">Expression representing the method to call</param>
    /// <returns>Continuation job ID</returns>
    string ContinueWith<T>(string parentJobId, Expression<Action<T>> methodCall);

    /// <summary>
    /// Create a continuation job that executes after parent job completes (async version)
    /// </summary>
    /// <typeparam name="T">Service type containing the method to execute</typeparam>
    /// <param name="parentJobId">The parent job ID</param>
    /// <param name="methodCall">Expression representing the async method to call</param>
    /// <returns>Continuation job ID</returns>
    string ContinueWith<T>(string parentJobId, Expression<Func<T, Task>> methodCall);
}
