using Microsoft.AspNetCore.Builder;

namespace MMO.Core.Middlewares.Jwt;

/// <summary>
/// Extension methods for registering JwtMiddleware
/// </summary>
public static class JwtMiddlewareExtensions
{
    /// <summary>
    /// Adds JWT middleware to extract user information from JWT tokens
    /// </summary>
    public static IApplicationBuilder UseJwtMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<JwtMiddleware>();
    }
}
