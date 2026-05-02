using Microsoft.Extensions.DependencyInjection;
using MagicTree.Framework.Services.JwtService;

namespace MagicTree.Framework.Services.Extensions;

/// <summary>
/// Extension methods for registering JWT service
/// </summary>
public static class JwtServiceExtensions
{
    /// <summary>
    /// Registers JWT service for accessing current user information
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddJwtService(this IServiceCollection services)
    {
        services.AddScoped<IJwtService, Jwt.JwtService>();
        return services;
    }
}
