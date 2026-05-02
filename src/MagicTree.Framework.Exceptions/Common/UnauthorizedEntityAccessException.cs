using MagicTree.Framework.Exceptions.Base;

namespace MagicTree.Framework.Exceptions.Common;

/// <summary>
/// Generic exception thrown when user lacks permission to access an entity.
/// Mapped to HTTP 403 Forbidden.
/// </summary>
public class UnauthorizedEntityAccessException : DomainException
{
    public UnauthorizedEntityAccessException(string entityName, string userId, string requiredPermission) 
        : base($"User '{userId}' does not have permission to access {entityName}. Required: {requiredPermission}", 
               $"UNAUTHORIZED_{entityName.ToUpperInvariant()}_ACCESS")
    {
        AddDetail("EntityName", entityName);
        AddDetail("UserId", userId);
        AddDetail("RequiredPermission", requiredPermission);
    }
}
