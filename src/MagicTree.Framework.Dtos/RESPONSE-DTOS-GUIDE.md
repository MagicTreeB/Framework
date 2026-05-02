# MagicTree.Framework.Dtos Response DTOs Usage Guide

## Overview

The `ErrorResponseDto` and `SuccessResponseDto` classes provide standardized response formats for all APIs in the MMO project. This ensures consistent API responses across all microservices.

## Response Structure

### Success Response Structure

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "statusCode": 200,
  "timestamp": "2025-12-10T10:30:00Z",
  "traceId": "abc123def456",
  "data": { /* your data here */ },
  "metadata": {
    "pagination": {
      "page": 1,
      "pageSize": 10,
      "totalCount": 100,
      "totalPages": 10
    }
  }
}
```

### Error Response Structure

```json
{
  "success": false,
  "message": "Validation failed",
  "statusCode": 400,
  "timestamp": "2025-12-10T10:30:00Z",
  "traceId": "abc123def456",
  "errorCode": "VALIDATION_ERROR",
  "errors": [
    "Email: Email is required",
    "Password: Password must be at least 8 characters"
  ],
  "metadata": {
    "fieldErrors": {
      "Email": ["Email is required"],
      "Password": ["Password must be at least 8 characters"]
    }
  }
}
```

## Usage Examples

### 1. Success Response - Simple (No Data)

**Scenario:** Logout, Delete, Email sent confirmation

```csharp
// In your endpoint
public async Task<IResult> Logout(HttpContext context)
{
    // ... logout logic
    
    var response = SuccessResponseDto.Ok("Logged out successfully");
    return Results.Ok(response);
}

// Response:
// {
//   "success": true,
//   "message": "Logged out successfully",
//   "statusCode": 200,
//   "timestamp": "2025-12-10T10:30:00Z"
// }
```

### 2. Success Response - With Data (Single Object)

**Scenario:** Get user by ID, Get product details

```csharp
// In your query handler
public async Task<IResult> GetUserById(Guid id)
{
    var user = await _userRepository.GetByIdAsync(id);
    if (user == null)
    {
        var errorResponse = ErrorResponseDto.NotFound("User not found");
        return Results.NotFound(errorResponse);
    }
    
    var userDto = user.ToDto();
    var response = SuccessResponseDto<UserDto>.Ok(userDto, "User retrieved successfully");
    return Results.Ok(response);
}

// Response:
// {
//   "success": true,
//   "message": "User retrieved successfully",
//   "statusCode": 200,
//   "timestamp": "2025-12-10T10:30:00Z",
//   "data": {
//     "id": "123e4567-e89b-12d3-a456-426614174000",
//     "username": "john.doe",
//     "email": "john@example.com"
//   }
// }
```

### 3. Success Response - With Data (List)

**Scenario:** Get all products, Search coupons

```csharp
// In your query handler
public async Task<IResult> GetAllProducts()
{
    var products = await _productRepository.GetAllAsync();
    var productDtos = products.Select(p => p.ToDto()).ToList();
    
    var response = SuccessResponseDto<List<ProductDto>>.Ok(
        productDtos, 
        "Products retrieved successfully"
    );
    return Results.Ok(response);
}

// Response:
// {
//   "success": true,
//   "message": "Products retrieved successfully",
//   "statusCode": 200,
//   "timestamp": "2025-12-10T10:30:00Z",
//   "data": [
//     { "id": "...", "name": "Product 1" },
//     { "id": "...", "name": "Product 2" }
//   ]
// }
```

### 4. Success Response - With Pagination

**Scenario:** Get paginated list of users, products, orders

```csharp
// In your query handler
public async Task<IResult> GetUsersPaginated(int page, int pageSize)
{
    var (users, totalCount) = await _userRepository.GetPaginatedAsync(page, pageSize);
    var userDtos = users.Select(u => u.ToDto()).ToList();
    
    var response = SuccessResponseDto<List<UserDto>>.OkWithPagination(
        userDtos,
        page,
        pageSize,
        totalCount,
        "Users retrieved successfully"
    );
    return Results.Ok(response);
}

// Response:
// {
//   "success": true,
//   "message": "Users retrieved successfully",
//   "statusCode": 200,
//   "timestamp": "2025-12-10T10:30:00Z",
//   "data": [ /* user objects */ ],
//   "metadata": {
//     "pagination": {
//       "page": 1,
//       "pageSize": 10,
//       "totalCount": 100,
//       "totalPages": 10,
//       "hasNextPage": true,
//       "hasPreviousPage": false
//     }
//   }
// }
```

### 5. Success Response - Created (201)

**Scenario:** Create user, Create product, Register

```csharp
// In your command handler
public async Task<IResult> RegisterUser(RegisterUserCommand command)
{
    var user = new User { /* ... */ };
    await _userRepository.AddAsync(user);
    await _unitOfWork.SaveChangesAsync();
    
    var userDto = user.ToDto();
    var response = SuccessResponseDto<UserDto>.Created(
        userDto, 
        "User registered successfully"
    );
    return Results.Created($"/api/users/{user.Id}", response);
}

// Response: HTTP 201 Created
// Location: /api/users/123e4567-e89b-12d3-a456-426614174000
// {
//   "success": true,
//   "message": "User registered successfully",
//   "statusCode": 201,
//   "timestamp": "2025-12-10T10:30:00Z",
//   "data": { /* user data */ }
// }
```

### 6. Success Response - Accepted (202) for Async Operations

**Scenario:** Background job triggered, Email queue, File processing

```csharp
// In your command handler
public async Task<IResult> ProcessLargeFile(ProcessFileCommand command)
{
    var jobId = BackgroundJob.Enqueue<IFileProcessor>(x => x.ProcessAsync(command.FileId));
    
    var response = SuccessResponseDto<string>.Accepted(
        jobId,
        "File processing started. Check job status for completion."
    );
    return Results.Accepted($"/api/jobs/{jobId}", response);
}

// Response: HTTP 202 Accepted
// Location: /api/jobs/abc123
// {
//   "success": true,
//   "message": "File processing started. Check job status for completion.",
//   "statusCode": 202,
//   "timestamp": "2025-12-10T10:30:00Z",
//   "data": "abc123"
// }
```

### 7. Error Response - BadRequest (400)

**Scenario:** Invalid input, Missing required fields

```csharp
// In your endpoint
public async Task<IResult> CreateProduct(CreateProductCommand command)
{
    if (string.IsNullOrWhiteSpace(command.Name))
    {
        var errorResponse = ErrorResponseDto.BadRequest(
            "Product name is required",
            new List<string> { "Name field cannot be empty" },
            "INVALID_PRODUCT_NAME"
        );
        return Results.BadRequest(errorResponse);
    }
    
    // ... create product
}

// Response: HTTP 400 Bad Request
// {
//   "success": false,
//   "message": "Product name is required",
//   "statusCode": 400,
//   "timestamp": "2025-12-10T10:30:00Z",
//   "errorCode": "INVALID_PRODUCT_NAME",
//   "errors": ["Name field cannot be empty"]
// }
```

### 8. Error Response - Validation Errors

**Scenario:** FluentValidation failures, Model validation errors

```csharp
// In your endpoint with FluentValidation
public async Task<IResult> RegisterUser(RegisterUserCommand command)
{
    var validator = new RegisterUserCommandValidator();
    var validationResult = await validator.ValidateAsync(command);
    
    if (!validationResult.IsValid)
    {
        var fieldErrors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToList()
            );
        
        var errorResponse = ErrorResponseDto.ValidationError(
            "Validation failed",
            fieldErrors
        );
        return Results.BadRequest(errorResponse);
    }
    
    // ... register user
}

// Response: HTTP 400 Bad Request
// {
//   "success": false,
//   "message": "Validation failed",
//   "statusCode": 400,
//   "timestamp": "2025-12-10T10:30:00Z",
//   "errorCode": "VALIDATION_ERROR",
//   "errors": [
//     "Email: Email is required",
//     "Password: Password must be at least 8 characters",
//     "Password: Password must contain at least one uppercase letter"
//   ],
//   "metadata": {
//     "fieldErrors": {
//       "Email": ["Email is required"],
//       "Password": [
//         "Password must be at least 8 characters",
//         "Password must contain at least one uppercase letter"
//       ]
//     }
//   }
// }
```

### 9. Error Response - Unauthorized (401)

**Scenario:** Missing/invalid JWT token, Expired session

```csharp
// In your middleware or endpoint
public async Task<IResult> GetCurrentUser(HttpContext context)
{
    var jwtInfo = context.GetJwtInfo();
    if (jwtInfo == null)
    {
        var errorResponse = ErrorResponseDto.Unauthorized(
            "Authentication required. Please log in.",
            "MISSING_TOKEN"
        );
        return Results.Unauthorized(errorResponse); // Note: Returns JSON, not 401 challenge
    }
    
    // ... get user
}

// Response: HTTP 401 Unauthorized
// {
//   "success": false,
//   "message": "Authentication required. Please log in.",
//   "statusCode": 401,
//   "timestamp": "2025-12-10T10:30:00Z",
//   "errorCode": "MISSING_TOKEN"
// }
```

### 10. Error Response - Forbidden (403)

**Scenario:** Insufficient permissions, Role-based access denied

```csharp
// In your endpoint
public async Task<IResult> DeleteUser(Guid id, HttpContext context)
{
    var jwtInfo = context.GetJwtInfo();
    if (!jwtInfo.Roles.Contains("Admin"))
    {
        var errorResponse = ErrorResponseDto.Forbidden(
            "You do not have permission to delete users",
            "INSUFFICIENT_PERMISSIONS"
        );
        return Results.Forbid(); // Or Results.Json(errorResponse, statusCode: 403)
    }
    
    // ... delete user
}

// Response: HTTP 403 Forbidden
// {
//   "success": false,
//   "message": "You do not have permission to delete users",
//   "statusCode": 403,
//   "timestamp": "2025-12-10T10:30:00Z",
//   "errorCode": "INSUFFICIENT_PERMISSIONS"
// }
```

### 11. Error Response - NotFound (404)

**Scenario:** Resource doesn't exist, Invalid ID

```csharp
// In your endpoint
public async Task<IResult> GetProductById(Guid id)
{
    var product = await _productRepository.GetByIdAsync(id);
    if (product == null)
    {
        var errorResponse = ErrorResponseDto.NotFound(
            $"Product with ID {id} not found",
            "PRODUCT_NOT_FOUND"
        );
        return Results.NotFound(errorResponse);
    }
    
    // ... return product
}

// Response: HTTP 404 Not Found
// {
//   "success": false,
//   "message": "Product with ID 123e4567-e89b-12d3-a456-426614174000 not found",
//   "statusCode": 404,
//   "timestamp": "2025-12-10T10:30:00Z",
//   "errorCode": "PRODUCT_NOT_FOUND"
// }
```

### 12. Error Response - Conflict (409)

**Scenario:** Duplicate email, Username already exists

```csharp
// In your command handler
public async Task<IResult> RegisterUser(RegisterUserCommand command)
{
    var existingUser = await _userRepository.GetByEmailAsync(command.Email);
    if (existingUser != null)
    {
        var errorResponse = ErrorResponseDto.Conflict(
            $"User with email {command.Email} already exists",
            "DUPLICATE_EMAIL"
        );
        return Results.Conflict(errorResponse);
    }
    
    // ... register user
}

// Response: HTTP 409 Conflict
// {
//   "success": false,
//   "message": "User with email john@example.com already exists",
//   "statusCode": 409,
//   "timestamp": "2025-12-10T10:30:00Z",
//   "errorCode": "DUPLICATE_EMAIL"
// }
```

### 13. Error Response - InternalServerError (500)

**Scenario:** Unexpected exception, Database error

```csharp
// In your global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionHandlerFeature?.Error;
        
        _logger.LogError(exception, "Unhandled exception occurred");
        
        var errorResponse = ErrorResponseDto.InternalServerError(
            "An unexpected error occurred. Please try again later.",
            "INTERNAL_ERROR"
        );
        errorResponse.TraceId = context.TraceIdentifier;
        
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(errorResponse);
    });
});

// Response: HTTP 500 Internal Server Error
// {
//   "success": false,
//   "message": "An unexpected error occurred. Please try again later.",
//   "statusCode": 500,
//   "timestamp": "2025-12-10T10:30:00Z",
//   "errorCode": "INTERNAL_ERROR",
//   "traceId": "0HN4K3QJKR2T0:00000001"
// }
```

## Integration with BasedEndpoints Pattern

```csharp
// In your endpoint file
public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.CreateGroup("/api/users", "Users");

        // Success with data
        group.MapGet<GetUserQuery, UserDto>("/{id:guid}")
            .WithMetadata("GetUser", "Get user by ID")
            .Produces<SuccessResponseDto<UserDto>>(200)  // Document success response
            .Produces<ErrorResponseDto>(404);             // Document error response

        // Success without data
        group.MapDelete<DeleteUserCommand, bool>("/{id:guid}")
            .WithMetadata("DeleteUser", "Delete user")
            .Produces<SuccessResponseDto>(200)
            .Produces<ErrorResponseDto>(404)
            .Produces<ErrorResponseDto>(403);

        // Custom endpoint with response DTOs
        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                var errorResponse = ErrorResponseDto.Unauthorized(
                    "Invalid email or password",
                    "INVALID_CREDENTIALS"
                );
                return Results.Json(errorResponse, statusCode: 401);
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                var errorResponse = ErrorResponseDto.Unauthorized(
                    "Invalid email or password",
                    "INVALID_CREDENTIALS"
                );
                return Results.Json(errorResponse, statusCode: 401);
            }

            var loginResponse = new LoginResponse { /* ... */ };
            var successResponse = SuccessResponseDto<LoginResponse>.Ok(
                loginResponse,
                "Login successful"
            );
            return Results.Ok(successResponse);
        })
        .WithMetadata("Login", "User login")
        .Produces<SuccessResponseDto<LoginResponse>>(200)
        .Produces<ErrorResponseDto>(401);
    }
}
```

## Adding TraceId from HttpContext

```csharp
// Extension method to add trace ID
public static class ResponseDtoExtensions
{
    public static T WithTraceId<T>(this T response, HttpContext context) where T : SuccessResponseDto
    {
        response.TraceId = context.TraceIdentifier;
        return response;
    }
    
    public static ErrorResponseDto WithTraceId(this ErrorResponseDto response, HttpContext context)
    {
        response.TraceId = context.TraceIdentifier;
        return response;
    }
}

// Usage
public async Task<IResult> GetUser(Guid id, HttpContext context)
{
    var user = await _userRepository.GetByIdAsync(id);
    if (user == null)
    {
        var errorResponse = ErrorResponseDto.NotFound("User not found")
            .WithTraceId(context);
        return Results.NotFound(errorResponse);
    }
    
    var response = SuccessResponseDto<UserDto>.Ok(user.ToDto())
        .WithTraceId(context);
    return Results.Ok(response);
}
```

## Custom Metadata Examples

```csharp
// Add cache info
var response = SuccessResponseDto<ProductDto>.Ok(productDto);
response.Metadata = new Dictionary<string, object>
{
    ["cacheHit"] = true,
    ["cacheExpiry"] = DateTime.UtcNow.AddMinutes(5)
};

// Add rate limit info
var errorResponse = ErrorResponseDto.BadRequest("Rate limit exceeded");
errorResponse.Metadata = new Dictionary<string, object>
{
    ["retryAfter"] = 60,
    ["limit"] = 10,
    ["remaining"] = 0
};

// Add execution time
var response = SuccessResponseDto<List<UserDto>>.Ok(users);
response.Metadata = new Dictionary<string, object>
{
    ["executionTimeMs"] = 123,
    ["queriedAt"] = DateTime.UtcNow
};
```

## Best Practices

1. ✅ **Always use response DTOs** for consistent API responses
2. ✅ **Add trace IDs** for debugging and log correlation
3. ✅ **Use appropriate status codes** - match ErrorResponseDto/SuccessResponseDto status codes with HTTP response codes
4. ✅ **Provide meaningful messages** - help clients understand what happened
5. ✅ **Use error codes** for client-side error handling (e.g., showing specific UI messages)
6. ✅ **Include field errors** for validation failures
7. ✅ **Add metadata** for pagination, rate limits, cache info, etc.
8. ✅ **Document responses** in OpenAPI with `.Produces<T>()` attributes
9. ✅ **Log errors with trace IDs** for debugging
10. ✅ **Never expose sensitive info** in error messages (stack traces, connection strings, etc.)

## OpenAPI Documentation

```csharp
// In your endpoint configuration
builder.Services.AddOpenApi();

// In your endpoints
group.MapGet<GetUsersQuery, List<UserDto>>("/")
    .WithMetadata("GetAllUsers", "Get all users")
    .Produces<SuccessResponseDto<List<UserDto>>>(200, "application/json")
    .Produces<ErrorResponseDto>(400, "application/json")
    .Produces<ErrorResponseDto>(401, "application/json")
    .Produces<ErrorResponseDto>(500, "application/json");
```

This will generate proper OpenAPI documentation showing response schemas for both success and error cases.

## Summary

The `ErrorResponseDto` and `SuccessResponseDto` classes provide:

- ✅ Consistent response structure across all APIs
- ✅ Static factory methods for common HTTP status codes
- ✅ Generic support for typed data payloads
- ✅ Pagination support out of the box
- ✅ Validation error support with field-specific errors
- ✅ Trace ID support for debugging
- ✅ Metadata support for additional context
- ✅ Easy integration with Minimal APIs and CQRS pattern

Use these DTOs in all your API responses to ensure consistency and improve client-side error handling.
