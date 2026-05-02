namespace MagicTree.Framework.Middlewares.MultiTenancy;

/// <summary>
/// Tenant context information extracted from JWT claims.
/// Stored in HttpContext.Items for DbContext to access.
/// </summary>
public class TenantContext
{
    /// <summary>
    /// Whether the current user has Host role (bypasses tenant filtering)
    /// </summary>
    public bool IsHost { get; set; }
    
    /// <summary>
    /// The OrganizationId of the current user's tenant
    /// </summary>
    public Guid? OrganizationId { get; set; }
    
    /// <summary>
    /// The current user's ID from JWT claims
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// HttpContext.Items key for storing tenant context
    /// </summary>
    public const string ContextKey = "TenantContext";
}
