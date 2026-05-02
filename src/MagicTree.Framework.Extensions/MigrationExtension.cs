using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace MagicTree.Framework.Extensions;

/// <summary>
/// Extension methods for automatic database migration
/// </summary>
public static class MigrationExtension
{
    /// <summary>
    /// Applies pending migrations automatically if AutoMigration feature is enabled
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type to migrate</typeparam>
    public static async Task<IApplicationBuilder> UseMigrationAsync<TDbContext>(this IApplicationBuilder app) 
        where TDbContext : DbContext
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var featureManager = services.GetRequiredService<IFeatureManager>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("MigrationExtension");

        // Check if AutoMigration feature is enabled
        var isAutoMigrationEnabled = await featureManager.IsEnabledAsync("AutoMigration");

        if (!isAutoMigrationEnabled)
        {
            logger.LogInformation("Auto-migration is disabled. Skipping database migration for {DbContext}.", typeof(TDbContext).Name);
            return app;
        }

        try
        {
            logger.LogInformation("Auto-migration is enabled. Starting database migration for {DbContext}...", typeof(TDbContext).Name);
            
            var dbContext = services.GetRequiredService<TDbContext>();
            
            // Get pending migrations
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            var pendingMigrationsList = pendingMigrations.ToList();

            if (pendingMigrationsList.Any())
            {
                logger.LogInformation("Found {Count} pending migration(s) for {DbContext}: {Migrations}", 
                    pendingMigrationsList.Count,
                    typeof(TDbContext).Name,
                    string.Join(", ", pendingMigrationsList));

                // Apply pending migrations
                await dbContext.Database.MigrateAsync();
                
                logger.LogInformation("Database migration completed successfully for {DbContext}!", typeof(TDbContext).Name);
            }
            else
            {
                logger.LogInformation("Database {DbContext} is up to date. No pending migrations.", typeof(TDbContext).Name);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database {DbContext}.", typeof(TDbContext).Name);
            throw; // Re-throw to prevent application startup with failed migration
        }

        return app;
    }

    /// <summary>
    /// Checks if database can be connected
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type to check</typeparam>
    public static async Task<bool> CanConnectToDatabaseAsync<TDbContext>(this IApplicationBuilder app) 
        where TDbContext : DbContext
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("MigrationExtension");

        try
        {
            var dbContext = services.GetRequiredService<TDbContext>();
            var canConnect = await dbContext.Database.CanConnectAsync();
            
            if (canConnect)
            {
                logger.LogInformation("Successfully connected to the database {DbContext}.", typeof(TDbContext).Name);
            }
            else
            {
                logger.LogWarning("Cannot connect to the database {DbContext}.", typeof(TDbContext).Name);
            }

            return canConnect;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking database connection for {DbContext}.", typeof(TDbContext).Name);
            return false;
        }
    }
}
