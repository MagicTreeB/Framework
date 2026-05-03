namespace MagicTree.Framework.Dtos;

public record SuccessResponseDto : BasedResponseDto
{
    public SuccessResponseDto()
    {
        Success = true;
        StatusCode = StatusCode.Success;
    }

    public static SuccessResponseDto Ok(string message = "Operation completed successfully")
        => new() { Success = true, Message = message, StatusCode = StatusCode.Success };

    public static SuccessResponseDto Created(string message = "Resource created successfully")
        => new() { Success = true, Message = message, StatusCode = StatusCode.Created };

    public static SuccessResponseDto Accepted(string message = "Request accepted for processing")
        => new() { Success = true, Message = message, StatusCode = StatusCode.Accepted };
}

public record SuccessResponseDto<T> : SuccessResponseDto
{
    public T? Data { get; set; }

    public static SuccessResponseDto<T> Ok(T data, string message = "Operation completed successfully")
        => new() { Success = true, Data = data, Message = message, StatusCode = StatusCode.Success };

    public static SuccessResponseDto<T> Created(T data, string message = "Resource created successfully")
        => new() { Success = true, Data = data, Message = message, StatusCode = StatusCode.Created };

    public static SuccessResponseDto<T> Accepted(T data, string message = "Request accepted for processing")
        => new() { Success = true, Data = data, Message = message, StatusCode = StatusCode.Accepted };

    public static SuccessResponseDto<T> OkWithPagination(
        T data,
        int page,
        int pageSize,
        int totalCount,
        string message = "Data retrieved successfully")
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new()
        {
            Success = true,
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
