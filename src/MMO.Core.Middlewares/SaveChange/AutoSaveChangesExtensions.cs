using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MMO.Core.Middlewares.SaveChange;

/// <summary>
/// Extension methods for registering AutoSaveChanges middleware
/// </summary>
public static class AutoSaveChangesExtensions
{
    /// <summary>
    /// Adds DbContext tracker service for auto-save functionality
    /// </summary>
    public static IServiceCollection AddAutoSaveChanges(this IServiceCollection services)
    {
        services.AddScoped<IDbContextTracker, DbContextTracker>();
        return services;
    }

    /// <summary>
    /// Adds middleware to automatically save DbContext changes after successful mutating requests
    /// </summary>
    public static IApplicationBuilder UseAutoSaveChanges(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AutoSaveChangesMiddleware>();
    }
}
