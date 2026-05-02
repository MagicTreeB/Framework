using Microsoft.AspNetCore.Http;
using MagicTree.Framework.Middlewares.Jwt;

namespace MagicTree.Framework.Middlewares.MultiTenancy;

/// <summary>
/// Middleware that automatically applies OrganizationId filters to EF Core queries
/// based on the current user's role and organization.
/// Host role bypasses filtering, all other roles get filtered.
/// </summary>
public class TenantQueryFilterMiddleware
{
    private readonly RequestDelegate _next;

    public TenantQueryFilterMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get JWT info from context (set by JwtMiddleware)
        var jwtInfo = context.GetJwtInfo();
        
        // Store tenant context for DbContext to access
        if (jwtInfo != null)
        {
            var tenantContext = new TenantContext
            {
                IsHost = jwtInfo.Roles.Contains("Host"),
                OrganizationId = GetOrganizationIdFromClaims(jwtInfo),
                UserId = jwtInfo.UserId
            };
            
            // Store in HttpContext.Items for DbContext to read
            context.Items[TenantContext.ContextKey] = tenantContext;
        }
        else
        {
            // No authenticated user - create empty context
            context.Items[TenantContext.ContextKey] = new TenantContext();
        }
        
        await _next(context);
    }
    
    /// <summary>
    /// Extracts OrganizationId from JWT claims.
    /// Looks for "OrganizationId" or "organization_id" claim.
    /// </summary>
    private Guid? GetOrganizationIdFromClaims(JwtInfo jwtInfo)
    {
        // Try standard claim name
        if (jwtInfo.Claims.TryGetValue("OrganizationId", out var orgIdStr) 
            && Guid.TryParse(orgIdStr, out var orgId))
        {
            return orgId;
        }
        
        // Try lowercase variant
        if (jwtInfo.Claims.TryGetValue("organization_id", out orgIdStr) 
            && Guid.TryParse(orgIdStr, out orgId))
        {
            return orgId;
        }
        
        return null;
    }
}
