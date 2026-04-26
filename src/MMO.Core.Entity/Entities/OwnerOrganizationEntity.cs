
using MMO.Core.Share;

namespace MMO.Core.Entity.Entities;

/// <summary>
/// Base entity for multi-tenant data isolation.
/// Entities inheriting this will be automatically filtered by OrganizationId
/// except for Host role users.
/// Combines BaseEntity properties with OrganizationId for tenant isolation.
/// </summary>
public abstract class OwnerOrganizationEntity<T> : BaseEntity<T>
{
    public OwnerOrganizationEntity(
        T id, 
        Guid organizationId,
        string createdBy,
        string createdByName = ShareKey.SystemUser
        ) : base(id, createdBy, createdByName)
    {
        OrganizationId = organizationId;
    }

    /// <summary>
    /// Organization/Tenant identifier for data isolation.
    /// All queries will be automatically filtered by this value
    /// unless the user has Host role.
    /// </summary>
    public Guid OrganizationId { get; set; }
    
    public void SetOrganization(Guid organizationId)
    {
        OrganizationId = organizationId;
    }
}
