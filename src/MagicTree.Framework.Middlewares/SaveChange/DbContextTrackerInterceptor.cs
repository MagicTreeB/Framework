using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MagicTree.Framework.Middlewares.SaveChange;

/// <summary>
/// Interceptor to automatically track DbContext instances for auto-save
/// Tracks context when SaveChanges is called (or before first query)
/// </summary>
public class DbContextTrackerInterceptor : SaveChangesInterceptor
{
    private readonly IDbContextTracker _tracker;
    private bool _isTracked;

    public DbContextTrackerInterceptor(IDbContextTracker tracker)
    {
        _tracker = tracker;
    }

    private void EnsureTracked(DbContext? context)
    {
        if (!_isTracked && context != null)
        {
            _tracker.Track(context);
            _isTracked = true;
        }
    }

    // Track on SaveChanges
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        EnsureTracked(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        EnsureTracked(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // IMPORTANT: Also track when changes are detected (before SaveChanges)
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        EnsureTracked(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        EnsureTracked(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
