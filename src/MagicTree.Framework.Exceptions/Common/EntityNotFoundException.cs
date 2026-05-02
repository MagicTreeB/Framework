using MagicTree.Framework.Exceptions.Base;

namespace MagicTree.Framework.Exceptions.Common;

/// <summary>
/// Generic exception thrown when an entity is not found by ID or other identifier.
/// Mapped to HTTP 404 Not Found.
/// </summary>
/// <typeparam name="TKey">The type of the entity's identifier.</typeparam>
public class EntityNotFoundException<TKey> : DomainException
{
    public EntityNotFoundException(string entityName, TKey id) 
        : base($"{entityName} with ID '{id}' was not found.", $"{entityName.ToUpperInvariant()}_NOT_FOUND")
    {
        AddDetail("EntityName", entityName);
        AddDetail("EntityId", id!);
    }
    
    public EntityNotFoundException(string entityName, string propertyName, object propertyValue) 
        : base($"{entityName} with {propertyName} '{propertyValue}' was not found.", 
               $"{entityName.ToUpperInvariant()}_NOT_FOUND")
    {
        AddDetail("EntityName", entityName);
        AddDetail("PropertyName", propertyName);
        AddDetail("PropertyValue", propertyValue);
    }
}
