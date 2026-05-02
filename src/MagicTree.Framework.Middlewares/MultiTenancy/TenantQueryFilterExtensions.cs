using Microsoft.AspNetCore.Builder;

namespace MagicTree.Framework.Middlewares.MultiTenancy;

/// <summary>
/// Extension methods for registering multi-tenancy middleware
/// </summary>
public static class TenantQueryFilterExtensions
{
    /// <summary>
    /// Adds automatic tenant query filtering based on user's OrganizationId.
    /// Host role bypasses filtering, all other roles (Admin, Staff, Client) get filtered.
    /// 
    /// IMPORTANT: Must be called AFTER UseJwtMiddleware and BEFORE database access.
    /// 
    /// Usage in Program.cs:
    /// <code>
    /// app.UseJwtMiddleware();
    /// app.UseTenantQueryFilter(); // Add here
    /// app.UseAuthentication();
    /// app.UseAuthorization();
    /// </code>
    /// </summary>
    public static IApplicationBuilder UseTenantQueryFilter(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantQueryFilterMiddleware>();
    }
}
