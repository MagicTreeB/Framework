
namespace MagicTree.Framework.Dtos;

/// <summary>
/// Standardized error response DTO for all API error responses.
/// Provides consistent error structure with support for validation errors, multiple error messages, and metadata.
/// </summary>
public record ErrorResponseDto : BasedResponseDto
{
    /// <summary>
    /// Collection of detailed error messages.
    /// Used for validation errors, multiple field errors, or additional error context.
    /// Example: ["Email is required", "Password must be at least 8 characters"]
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Optional error code for client-side error handling.
    /// Example: "INVALID_EMAIL", "EXPIRED_TOKEN", "INSUFFICIENT_PERMISSIONS"
    /// </summary>
    public string? ErrorCode { get; set; }

    public ErrorResponseDto()
    {
        Success = false;
    }

    public ErrorResponseDto(string message)
        : this()
    {
        Success = false;
        Message = message;
    }

    /// <summary>
    /// Creates a BadRequest (400) error response.
    /// </summary>
    public static ErrorResponseDto BadRequest(string message, List<string>? errors = null, string? errorCode = null)
    {
        return new ErrorResponseDto
        {
            Message = message,
            StatusCode = StatusCode.BadRequest,
            Errors = errors ?? new List<string>(),
            ErrorCode = errorCode
        };
    }

    /// <summary>
    /// Creates an Unauthorized (401) error response.
    /// </summary>
    public static ErrorResponseDto Unauthorized(string message = "Unauthorized access", string? errorCode = null)
    {
        return new ErrorResponseDto
        {
            Message = message,
            StatusCode = StatusCode.Unauthorized,
            ErrorCode = errorCode ?? "UNAUTHORIZED"
        };
    }

    /// <summary>
    /// Creates a Forbidden (403) error response.
    /// </summary>
    public static ErrorResponseDto Forbidden(string message = "Access forbidden", string? errorCode = null)
    {
        return new ErrorResponseDto
        {
            Message = message,
            StatusCode = StatusCode.Forbidden,
            ErrorCode = errorCode ?? "FORBIDDEN"
        };
    }

    /// <summary>
    /// Creates a NotFound (404) error response.
    /// </summary>
    public static ErrorResponseDto NotFound(string message = "Resource not found", string? errorCode = null)
    {
        return new ErrorResponseDto
        {
            Message = message,
            StatusCode = StatusCode.NotFound,
            ErrorCode = errorCode ?? "NOT_FOUND"
        };
    }


    /// <summary>
    /// Creates an InternalServerError (500) error response.
    /// </summary>
    public static ErrorResponseDto InternalServerError(string message = "An internal server error occurred", string? errorCode = null)
    {
        return new ErrorResponseDto
        {
            Message = message,
            StatusCode = StatusCode.InternalServerError,
            ErrorCode = errorCode ?? "INTERNAL_ERROR"
        };
    }

    /// <summary>
    /// Creates a validation error response with field-specific errors.
    /// </summary>
    public static ErrorResponseDto ValidationError(string message, Dictionary<string, List<string>> fieldErrors)
    {
        var allErrors = fieldErrors.SelectMany(kvp => 
            kvp.Value.Select(error => $"{kvp.Key}: {error}")
        ).ToList();

        return new ErrorResponseDto
        {
            Message = message,
            StatusCode = StatusCode.BadRequest,
            Errors = allErrors,
            ErrorCode = "VALIDATION_ERROR",
            Metadata = new Dictionary<string, object>
            {
                ["fieldErrors"] = fieldErrors
            }
        };
    }
}
