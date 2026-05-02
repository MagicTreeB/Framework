using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MagicTree.Framework.Middlewares.SaveChange;

/// <summary>
/// Middleware to automatically save DbContext changes after successful request processing
/// </summary>
public class AutoSaveChangesMiddleware
{
    private readonly RequestDelegate _next;

    public AutoSaveChangesMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Execute the request pipeline
        await _next(context);

        // Only auto-save for successful responses (200-299)
        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            // Check if the request was a mutating operation (POST, PUT, PATCH, DELETE)
            var method = context.Request.Method.ToUpperInvariant();
            if (method == "POST" || method == "PUT" || method == "PATCH" || method == "DELETE")
            {
                Console.WriteLine($"[DEBUG] AutoSaveChanges: Mutating operation detected ({method}). Attempting to save changes...");
                await SaveChangesAsync(context);
            }
            else
            {
                Console.WriteLine($"[DEBUG] AutoSaveChanges: Non-mutating operation ({method}). Skipping save.");
            }
        }
    }

    private async Task SaveChangesAsync(HttpContext context)
    {
        try
        {
            var serviceProvider = context.RequestServices;
            
            // Try to get the DbContext tracker if registered
            var tracker = serviceProvider.GetService<IDbContextTracker>();
            if (tracker != null)
            {
                Console.WriteLine("[DEBUG] AutoSaveChanges: Tracker found. Calling SaveAllChangesAsync...");
                await tracker.SaveAllChangesAsync();
                Console.WriteLine("[DEBUG] AutoSaveChanges: SaveAllChangesAsync completed successfully.");
                return;
            }
            
            Console.WriteLine("[DEBUG] AutoSaveChanges: No tracker found. Using fallback method...");
            
            // Fallback: Get all types that inherit from DbContext from the entry assembly and its references
            var assembly = Assembly.GetEntryAssembly();
            if (assembly == null) return;
            
            var dbContextTypes = assembly.GetReferencedAssemblies()
                .Select(name =>
                {
                    try { return Assembly.Load(name); }
                    catch { return null; }
                })
                .Where(a => a != null)
                .Concat(new[] { assembly })
                .SelectMany(a =>
                {
                    try { return a!.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(DbContext)))
                .ToList();
            
            foreach (var dbContextType in dbContextTypes)
            {
                var dbContext = serviceProvider.GetService(dbContextType) as DbContext;
                if (dbContext != null && dbContext.ChangeTracker.HasChanges())
                {
                    await dbContext.SaveChangesAsync();
                }
            }
        }
        catch
        {
            // Silently fail if auto-save doesn't work
            // The handler should still work if they manually call SaveChangesAsync
        }
    }
}
