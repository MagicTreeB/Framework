using Microsoft.AspNetCore.Http;

namespace MMO.Core.Middlewares.MultiTenancy;

/// <summary>
/// Helper methods for accessing tenant context in application code
/// </summary>
public static class TenantContextAccessor
{
    /// <summary>
    /// Gets the current tenant context from HttpContext.
    /// Returns null if no context is available.
    /// </summary>
    public static TenantContext? GetTenantContext(this HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(TenantContext.ContextKey, out var context))
        {
            return context as TenantContext;
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets the current user's OrganizationId from tenant context.
    /// Returns null if not available.
    /// </summary>
    public static Guid? GetOrganizationId(this HttpContext httpContext)
    {
        return httpContext.GetTenantContext()?.OrganizationId;
    }
    
    /// <summary>
    /// Checks if the current user has Host role (bypasses tenant filtering).
    /// </summary>
    public static bool IsHostUser(this HttpContext httpContext)
    {
        return httpContext.GetTenantContext()?.IsHost ?? false;
    }
    
    /// <summary>
    /// Gets the OrganizationId for the current request, throwing exception if not found.
    /// Use this in command handlers where OrganizationId is required.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown when OrganizationId is not available</exception>
    public static Guid GetRequiredOrganizationId(this HttpContext httpContext)
    {
        var organizationId = httpContext.GetOrganizationId();
        
        if (!organizationId.HasValue)
        {
            throw new UnauthorizedAccessException(
                "OrganizationId not found in user claims. Ensure the user is authenticated and has an organization assigned.");
        }
        
        return organizationId.Value;
    }
}
