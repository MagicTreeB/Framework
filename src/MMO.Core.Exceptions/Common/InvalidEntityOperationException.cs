using MMO.Core.Exceptions.Base;

namespace MMO.Core.Exceptions.Common;

/// <summary>
/// Generic exception thrown when an invalid operation is attempted on an entity (e.g., state transition).
/// Mapped to HTTP 400 Bad Request.
/// </summary>
public class InvalidEntityOperationException : DomainException
{
    public InvalidEntityOperationException(string entityName, string operation, string currentState) 
        : base($"Cannot perform '{operation}' on {entityName} in '{currentState}' state.", 
               $"INVALID_{entityName.ToUpperInvariant()}_OPERATION")
    {
        AddDetail("EntityName", entityName);
        AddDetail("Operation", operation);
        AddDetail("CurrentState", currentState);
    }
}
