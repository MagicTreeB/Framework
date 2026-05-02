using Microsoft.EntityFrameworkCore;

namespace MagicTree.Framework.Middlewares.SaveChange;

/// <summary>
/// Interface for tracking and saving DbContext changes
/// </summary>
public interface IDbContextTracker
{
    /// <summary>
    /// Registers a DbContext instance to be tracked
    /// </summary>
    void Track(DbContext dbContext);

    /// <summary>
    /// Saves all changes for tracked DbContext instances
    /// </summary>
    Task SaveAllChangesAsync();
}

/// <summary>
/// Default implementation of DbContext tracker
/// </summary>
public class DbContextTracker : IDbContextTracker
{
    private readonly List<DbContext> _dbContexts = new();

    public void Track(DbContext dbContext)
    {
        if (!_dbContexts.Contains(dbContext))
        {
            _dbContexts.Add(dbContext);
        }
    }

    public async Task SaveAllChangesAsync()
    {
        Console.WriteLine($"[DEBUG] DbContextTracker: SaveAllChangesAsync called. Tracked contexts: {_dbContexts.Count}");
        
        foreach (var dbContext in _dbContexts)
        {
            try
            {
                var hasChanges = dbContext.ChangeTracker.HasChanges();
                Console.WriteLine($"[DEBUG] DbContextTracker: Context {dbContext.GetType().Name} has changes: {hasChanges}");
                
                if (hasChanges)
                {
                    Console.WriteLine($"[DEBUG] DbContextTracker: About to call SaveChangesAsync for {dbContext.GetType().Name}...");
                    var saved = await dbContext.SaveChangesAsync();
                    Console.WriteLine($"[DEBUG] DbContextTracker: Successfully saved {saved} changes to {dbContext.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] DbContextTracker: Failed to save changes for {dbContext.GetType().Name}");
                Console.WriteLine($"[ERROR] Exception: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[ERROR] InnerException: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                }
                throw; // Re-throw to let global exception handler deal with it
            }
        }
    }
}
