using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MMO.Core.Middlewares.NullResponse;

/// <summary>
/// Extension methods for registering NullResponseMiddleware
/// </summary>
public static class NullResponseMiddlewareExtensions
{
    /// <summary>
    /// Adds NullResponse middleware to intercept and handle null API responses
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="options">Optional configuration for null response handling</param>
    public static IApplicationBuilder UseNullResponseHandler(this IApplicationBuilder app, NullResponseOptions? options = null)
    {
        options ??= new NullResponseOptions();
        return app.UseMiddleware<NullResponseMiddleware>(options);
    }

    /// <summary>
    /// Adds NullResponse middleware with custom configuration
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="configureOptions">Action to configure options</param>
    public static IApplicationBuilder UseNullResponseHandler(this IApplicationBuilder app, Action<NullResponseOptions> configureOptions)
    {
        var options = new NullResponseOptions();
        configureOptions(options);
        return app.UseMiddleware<NullResponseMiddleware>(options);
    }
}
