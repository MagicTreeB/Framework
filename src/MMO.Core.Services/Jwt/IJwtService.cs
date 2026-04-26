using MMO.Core.Middlewares.Jwt;

namespace MMO.Core.Services.JwtService;

/// <summary>
/// JWT service for accessing current user information from HttpContext
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Gets JWT information from the current HttpContext
    /// </summary>
    /// <returns>JwtInfo if user is authenticated, null otherwise</returns>
    JwtInfo? GetJwtInfo();

    /// <summary>
    /// Gets the current user ID from JWT
    /// </summary>
    /// <returns>User ID if authenticated, null otherwise</returns>
    string GetUserId();

    /// <summary>
    /// Gets the current username from JWT
    /// </summary>
    /// <returns>Username if authenticated, null otherwise</returns>
    string GetUserName();

    /// <summary>
    /// Gets the current organization ID from JWT
    /// </summary>
    /// <returns>Organization ID if available, null otherwise</returns>
    Guid GetOrganizationId();
}
