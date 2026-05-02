using Microsoft.AspNetCore.Http;

namespace MagicTree.Framework.Middlewares.Jwt;

/// <summary>
/// Extension methods for accessing JWT info from HttpContext
/// </summary>
public static class HttpContextJwtExtensions
{
    /// <summary>
    /// Gets JWT information from HttpContext if available
    /// </summary>
    /// <returns>JwtInfo if user is authenticated and JWT was parsed, otherwise null</returns>
    public static JwtInfo? GetJwtInfo(this HttpContext context)
    {
        return context.Items.TryGetValue("JwtInfo", out var jwtInfo)
            ? jwtInfo as JwtInfo
            : null;
    }

    /// <summary>
    /// Gets the current user ID from JWT if available
    /// </summary>
    /// <returns>User ID if authenticated, otherwise null</returns>
    public static string? GetUserId(this HttpContext context)
    {
        return context.GetJwtInfo()?.UserId;
    }

    /// <summary>
    /// Gets the current username from JWT if available
    /// </summary>
    /// <returns>Username if authenticated, otherwise null</returns>
    public static string? GetUsername(this HttpContext context)
    {
        return context.GetJwtInfo()?.Username;
    }

    /// <summary>
    /// Gets the current user email from JWT if available
    /// </summary>
    /// <returns>Email if authenticated, otherwise null</returns>
    public static string? GetUserEmail(this HttpContext context)
    {
        return context.GetJwtInfo()?.Email;
    }

    /// <summary>
    /// Gets the current organization ID from JWT if available
    /// </summary>
    /// <returns>Organization ID if authenticated and present, otherwise null</returns>
    public static Guid? GetOrganizationId(this HttpContext context)
    {
        return context.GetJwtInfo()?.OrganizationId;
    }

    /// <summary>
    /// Gets the current organization ID from JWT, throws if not present
    /// </summary>
    /// <returns>Organization ID</returns>
    /// <exception cref="InvalidOperationException">Thrown if organization ID is not available</exception>
    public static Guid GetRequiredOrganizationId(this HttpContext context)
    {
        var orgId = context.GetOrganizationId();
        if (!orgId.HasValue)
        {
            throw new InvalidOperationException("Organization ID is not available in JWT token");
        }
        return orgId.Value;
    }

    /// <summary>
    /// Gets the current user roles from JWT if available
    /// </summary>
    /// <returns>List of roles if authenticated, otherwise empty list</returns>
    public static List<string> GetUserRoles(this HttpContext context)
    {
        return context.GetJwtInfo()?.Roles ?? new List<string>();
    }

    /// <summary>
    /// Gets the current user permissions from JWT if available
    /// </summary>
    /// <returns>List of permissions if authenticated, otherwise empty list</returns>
    public static List<string> GetUserPermissions(this HttpContext context)
    {
        return context.GetJwtInfo()?.Permissions ?? new List<string>();
    }

    /// <summary>
    /// Checks if the current user has a specific permission
    /// </summary>
    /// <param name="context">HttpContext</param>
    /// <param name="permission">Permission to check (e.g., "user.edit")</param>
    /// <returns>True if user has the permission, otherwise false</returns>
    public static bool HasPermission(this HttpContext context, string permission)
    {
        var permissions = context.GetUserPermissions();
        return permissions.Contains(permission);
    }

    /// <summary>
    /// Checks if the current user has any of the specified permissions
    /// </summary>
    /// <param name="context">HttpContext</param>
    /// <param name="permissions">Permissions to check</param>
    /// <returns>True if user has at least one of the permissions, otherwise false</returns>
    public static bool HasAnyPermission(this HttpContext context, params string[] permissions)
    {
        var userPermissions = context.GetUserPermissions();
        return permissions.Any(p => userPermissions.Contains(p));
    }

    /// <summary>
    /// Checks if the current user has all of the specified permissions
    /// </summary>
    /// <param name="context">HttpContext</param>
    /// <param name="permissions">Permissions to check</param>
    /// <returns>True if user has all of the permissions, otherwise false</returns>
    public static bool HasAllPermissions(this HttpContext context, params string[] permissions)
    {
        var userPermissions = context.GetUserPermissions();
        return permissions.All(p => userPermissions.Contains(p));
    }
}
