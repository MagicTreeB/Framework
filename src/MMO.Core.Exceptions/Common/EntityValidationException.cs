using MMO.Core.Exceptions.Base;

namespace MMO.Core.Exceptions.Common;

/// <summary>
/// Generic exception thrown when entity validation fails (business rule violation).
/// Mapped to HTTP 400 Bad Request.
/// </summary>
public class EntityValidationException : DomainException
{
    public EntityValidationException(string entityName, string message) 
        : base(message, $"{entityName.ToUpperInvariant()}_VALIDATION_ERROR")
    {
        AddDetail("EntityName", entityName);
    }
    
    public EntityValidationException(string entityName, Dictionary<string, string[]> validationErrors) 
        : base($"{entityName} validation failed.", $"{entityName.ToUpperInvariant()}_VALIDATION_ERROR")
    {
        AddDetail("EntityName", entityName);
        AddDetail("ValidationErrors", validationErrors);
    }
}
