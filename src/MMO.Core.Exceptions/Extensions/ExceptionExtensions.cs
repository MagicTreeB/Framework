using Microsoft.AspNetCore.Builder;
using MMO.Core.Exceptions.Middlewares;

namespace MMO.Core.Exceptions.Extensions;

/// <summary>
/// Extension methods for registering global exception handling middleware.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// Adds global exception handler middleware to the application pipeline.
    /// Should be added early in the pipeline, before UseAuthentication and UseAuthorization.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandler>();
    }
}
