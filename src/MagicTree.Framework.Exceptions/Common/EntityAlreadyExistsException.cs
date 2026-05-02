using MagicTree.Framework.Exceptions.Base;

namespace MagicTree.Framework.Exceptions.Common;

/// <summary>
/// Generic exception thrown when attempting to create an entity that already exists.
/// Mapped to HTTP 409 Conflict.
/// </summary>
public class EntityAlreadyExistsException : DomainException
{
    public EntityAlreadyExistsException(string entityName, string propertyName, object propertyValue) 
        : base($"{entityName} with {propertyName} '{propertyValue}' already exists.", 
               $"{entityName.ToUpperInvariant()}_ALREADY_EXISTS")
    {
        AddDetail("EntityName", entityName);
        AddDetail("PropertyName", propertyName);
        AddDetail("PropertyValue", propertyValue);
    }
}
