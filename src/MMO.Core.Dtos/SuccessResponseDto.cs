namespace MMO.Core.Dtos;

/// <summary>
/// Standardized success response DTO for all API success responses.
/// Provides consistent response structure with optional data payload and metadata.
/// </summary>
public record SuccessResponseDto : BasedResponseDto
{
    /// <summary>
    /// Timestamp when the response was generated (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;


    /// <summary>
    /// Creates an OK (200) success response without data.
    /// </summary>
    public static SuccessResponseDto Ok(string message = "Operation completed successfully")
    {
        return new SuccessResponseDto
        {
            Message = message,
            StatusCode = StatusCode.Success
        };
    }

    /// <summary>
    /// Creates a Created (201) success response.
    /// </summary>
    public static SuccessResponseDto Created(string message = "Resource created successfully")
    {
        return new SuccessResponseDto
        {
            Message = message,
            StatusCode = StatusCode.Created
        };
    }

    /// <summary>
    /// Creates an Accepted (202) success response for async operations.
    /// </summary>
    public static SuccessResponseDto Accepted(string message = "Request accepted for processing")
    {
        return new SuccessResponseDto
        {
            Message = message,
            StatusCode = StatusCode.Accepted
        };
    }
}

/// <summary>
/// Generic success response DTO with typed data payload.
/// Use this when you need to return data with the response.
/// </summary>
/// <typeparam name="T">Type of the data payload</typeparam>
public record SuccessResponseDto<T> : SuccessResponseDto
{
    /// <summary>
    /// The data payload returned by the API.
    /// Can be a single object, list, or complex type.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Creates an OK (200) success response with data.
    /// </summary>
    public static SuccessResponseDto<T> Ok(T data, string message = "Operation completed successfully")
    {
        return new SuccessResponseDto<T>
        {
            Data = data,
            Message = message,
            StatusCode = StatusCode.Success
        };
    }

    /// <summary>
    /// Creates a Created (201) success response with data.
    /// </summary>
    public static SuccessResponseDto<T> Created(T data, string message = "Resource created successfully")
    {
        return new SuccessResponseDto<T>
        {
            Data = data,
            Message = message,
            StatusCode = StatusCode.Created
        };
    }

    /// <summary>
    /// Creates an Accepted (202) success response with data.
    /// </summary>
    public static SuccessResponseDto<T> Accepted(T data, string message = "Request accepted for processing")
    {
        return new SuccessResponseDto<T>
        {
            Data = data,
            Message = message,
            StatusCode = StatusCode.Accepted
        };
    }

    /// <summary>
    /// Creates a success response with pagination metadata.
    /// </summary>
    public static SuccessResponseDto<T> OkWithPagination(
        T data, 
        int page, 
        int pageSize, 
        int totalCount, 
        string message = "Data retrieved successfully")
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        
        return new SuccessResponseDto<T>
        {
            Data = data,
            Message = message,
            StatusCode = StatusCode.Success,
            Metadata = new Dictionary<string, object>
            {
                ["pagination"] = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages,
                    hasNextPage = page < totalPages,
                    hasPreviousPage = page > 1
                }
            }
        };
    }
}
