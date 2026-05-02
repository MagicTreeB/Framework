namespace MagicTree.Framework.Exceptions.Base;

/// <summary>
/// Base exception for all domain-specific exceptions in the application.
/// Provides error code and detailed metadata for consistent error handling.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Unique error code for this exception (e.g., "USER_NOT_FOUND", "PRODUCT_ALREADY_EXISTS")
    /// </summary>
    public string ErrorCode { get; }
    
    /// <summary>
    /// Additional details about the exception (e.g., entity ID, validation errors)
    /// </summary>
    public Dictionary<string, object> Details { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="errorCode">The unique error code for this exception.</param>
    protected DomainException(string message, string errorCode) 
        : base(message)
    {
        ErrorCode = errorCode;
        Details = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="errorCode">The unique error code for this exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    protected DomainException(string message, string errorCode, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Details = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Adds additional detail to the exception metadata.
    /// </summary>
    /// <param name="key">The detail key.</param>
    /// <param name="value">The detail value.</param>
    public void AddDetail(string key, object value)
    {
        Details[key] = value;
    }
}
