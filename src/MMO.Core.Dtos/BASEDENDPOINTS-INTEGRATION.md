# Integrating Response DTOs with BasedEndpoints

## Overview

This guide shows how to update the `BasedEndpoints` helper class to automatically wrap CQRS responses in `SuccessResponseDto` and `ErrorResponseDto` for consistent API responses.

## Updated BasedEndpoints Implementation

### Option 1: Automatic Wrapping (Recommended)

Update your `BasedEndpoints.cs` to automatically wrap responses:

```csharp
using MMO.Core.Dtos;
using FluentResults;
using DKNet.Fluents.Requests;
using DKNet.Fluents.Queries;

namespace YourApi.ApiEndpoints;

public static class BasedEndpoints
{
    /// <summary>
    /// Maps a POST endpoint for a command that returns IResult<TResponse>.
    /// Automatically wraps response in SuccessResponseDto or ErrorResponseDto.
    /// </summary>
    public static RouteHandlerBuilder MapPost<TCommand, TResponse>(
        this RouteGroupBuilder group,
        string pattern)
        where TCommand : IWitResponse<TResponse>
    {
        return group.MapPost(pattern, async (
            TCommand command,
            IHandler<TCommand, TResponse> handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(command, ct);
            return ConvertToHttpResultWithDto(result, httpContext);
        });
    }

    /// <summary>
    /// Maps a GET endpoint for a query that returns TResponse.
    /// Automatically wraps response in SuccessResponseDto or ErrorResponseDto.
    /// </summary>
    public static RouteHandlerBuilder MapGet<TQuery, TResponse>(
        this RouteGroupBuilder group,
        string pattern)
        where TQuery : IWitResponse<TResponse>
    {
        return group.MapGet(pattern, async (
            [AsParameters] TQuery query,
            IHandler<TQuery, TResponse> handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var response = await handler.Handle(query, ct);
            if (response == null)
            {
                var errorResponse = ErrorResponseDto.NotFound("Resource not found")
                    .WithTraceId(httpContext);
                return Results.NotFound(errorResponse);
            }

            var successResponse = SuccessResponseDto<TResponse>.Ok(response)
                .WithTraceId(httpContext);
            return Results.Ok(successResponse);
        });
    }

    /// <summary>
    /// Maps a PUT endpoint for an update command.
    /// </summary>
    public static RouteHandlerBuilder MapPut<TCommand, TResponse>(
        this RouteGroupBuilder group,
        string pattern)
        where TCommand : IWitResponse<TResponse>
    {
        return group.MapPut(pattern, async (
            TCommand command,
            IHandler<TCommand, TResponse> handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(command, ct);
            return ConvertToHttpResultWithDto(result, httpContext);
        });
    }

    /// <summary>
    /// Maps a DELETE endpoint for a delete command.
    /// </summary>
    public static RouteHandlerBuilder MapDelete<TCommand, TResponse>(
        this RouteGroupBuilder group,
        string pattern)
        where TCommand : IWitResponse<TResponse>
    {
        return group.MapDelete(pattern, async (
            [AsParameters] TCommand command,
            IHandler<TCommand, TResponse> handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(command, ct);
            return ConvertToHttpResultWithDto(result, httpContext);
        });
    }

    /// <summary>
    /// Maps a PATCH endpoint for a partial update command.
    /// </summary>
    public static RouteHandlerBuilder MapPatch<TCommand, TResponse>(
        this RouteGroupBuilder group,
        string pattern)
        where TCommand : IWitResponse<TResponse>
    {
        return group.MapPatch(pattern, async (
            TCommand command,
            IHandler<TCommand, TResponse> handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(command, ct);
            return ConvertToHttpResultWithDto(result, httpContext);
        });
    }

    /// <summary>
    /// Converts FluentResults IResult to HTTP result with response DTOs.
    /// </summary>
    private static IResult ConvertToHttpResultWithDto<T>(IResult<T> result, HttpContext httpContext)
    {
        if (result.IsSuccess)
        {
            var successResponse = SuccessResponseDto<T>.Ok(result.Value)
                .WithTraceId(httpContext);
            return Results.Ok(successResponse);
        }

        // Extract error messages
        var errors = result.Errors.Select(e => e.Message).ToList();
        var mainMessage = errors.FirstOrDefault() ?? "An error occurred";

        // Check for specific error types
        if (errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
        {
            var errorResponse = ErrorResponseDto.NotFound(mainMessage, "NOT_FOUND");
            errorResponse.Errors = errors;
            errorResponse.TraceId = httpContext.TraceIdentifier;
            return Results.NotFound(errorResponse);
        }

        if (errors.Any(e => e.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)))
        {
            var errorResponse = ErrorResponseDto.Unauthorized(mainMessage);
            errorResponse.Errors = errors;
            errorResponse.TraceId = httpContext.TraceIdentifier;
            return Results.Json(errorResponse, statusCode: 401);
        }

        if (errors.Any(e => e.Contains("forbidden", StringComparison.OrdinalIgnoreCase)))
        {
            var errorResponse = ErrorResponseDto.Forbidden(mainMessage);
            errorResponse.Errors = errors;
            errorResponse.TraceId = httpContext.TraceIdentifier;
            return Results.Json(errorResponse, statusCode: 403);
        }

        if (errors.Any(e => e.Contains("conflict", StringComparison.OrdinalIgnoreCase) || 
                           e.Contains("already exists", StringComparison.OrdinalIgnoreCase)))
        {
            var errorResponse = ErrorResponseDto.Conflict(mainMessage);
            errorResponse.Errors = errors;
            errorResponse.TraceId = httpContext.TraceIdentifier;
            return Results.Conflict(errorResponse);
        }

        // Default to BadRequest for validation and other errors
        var badRequestResponse = ErrorResponseDto.BadRequest(mainMessage, errors);
        badRequestResponse.TraceId = httpContext.TraceIdentifier;
        return Results.BadRequest(badRequestResponse);
    }

    /// <summary>
    /// Creates a route group with a prefix and tag.
    /// </summary>
    public static RouteGroupBuilder CreateGroup(
        this IEndpointRouteBuilder app,
        string prefix,
        string tag)
    {
        return app.MapGroup(prefix).WithTags(tag);
    }

    /// <summary>
    /// Adds OpenAPI metadata to an endpoint.
    /// </summary>
    public static RouteHandlerBuilder WithMetadata(
        this RouteHandlerBuilder builder,
        string name,
        string summary,
        string? description = null)
    {
        builder.WithName(name)
               .WithSummary(summary);

        if (!string.IsNullOrWhiteSpace(description))
        {
            builder.WithDescription(description);
        }

        return builder;
    }

    /// <summary>
    /// Adds OpenAPI response documentation with response DTOs.
    /// </summary>
    public static RouteHandlerBuilder WithResponseDocs<TData>(
        this RouteHandlerBuilder builder,
        int successStatusCode = 200)
    {
        return builder
            .Produces<SuccessResponseDto<TData>>(successStatusCode, "application/json")
            .Produces<ErrorResponseDto>(400, "application/json")
            .Produces<ErrorResponseDto>(401, "application/json")
            .Produces<ErrorResponseDto>(404, "application/json")
            .Produces<ErrorResponseDto>(500, "application/json");
    }

    /// <summary>
    /// Adds OpenAPI response documentation for endpoints without data.
    /// </summary>
    public static RouteHandlerBuilder WithResponseDocs(
        this RouteHandlerBuilder builder,
        int successStatusCode = 200)
    {
        return builder
            .Produces<SuccessResponseDto>(successStatusCode, "application/json")
            .Produces<ErrorResponseDto>(400, "application/json")
            .Produces<ErrorResponseDto>(401, "application/json")
            .Produces<ErrorResponseDto>(404, "application/json")
            .Produces<ErrorResponseDto>(500, "application/json");
    }
}
```

### Extension Methods for TraceId

```csharp
namespace YourApi.Extensions;

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
```

## Usage in Endpoint Files

### Example 1: User Endpoints with Automatic Wrapping

```csharp
using YourApi.Application.Commands;
using YourApi.Application.Queries;
using YourApi.Contract.DTOs;

namespace YourApi.ApiEndpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.CreateGroup("/api/users", "Users");

        // POST - Register user
        // Response automatically wrapped in SuccessResponseDto<UserDto> or ErrorResponseDto
        group.MapPost<RegisterUserCommand, UserDto>("/register")
            .WithMetadata("RegisterUser", "Register a new user", 
                "Creates a new user account with the provided information")
            .WithResponseDocs<UserDto>(201)  // Documents SuccessResponseDto<UserDto> and ErrorResponseDto
            .AllowAnonymous();

        // GET - Get all users
        group.MapGet<GetAllUsersQuery, List<UserDto>>("/")
            .WithMetadata("GetAllUsers", "Get all users")
            .WithResponseDocs<List<UserDto>>()  // Documents response DTOs
            .RequireAuthorization("Admin");

        // GET - Get user by ID
        group.MapGet<GetUserQuery, UserDto>("/{id:guid}")
            .WithMetadata("GetUserById", "Get user by ID")
            .WithResponseDocs<UserDto>();

        // PUT - Update user
        group.MapPut<UpdateUserCommand, UserDto>("/{id:guid}")
            .WithMetadata("UpdateUser", "Update a user")
            .WithResponseDocs<UserDto>();

        // DELETE - Delete user
        group.MapDelete<DeleteUserCommand, bool>("/{id:guid}")
            .WithMetadata("DeleteUser", "Delete a user")
            .WithResponseDocs();  // No data in response
    }
}
```

### Example 2: Command Handler with Explicit Error Handling

```csharp
using FluentResults;
using YourApi.Domain.Entities;
using YourApi.Domain.Repositories;

namespace YourApi.Application.Commands;

public class RegisterUserCommandHandler : IHandler<RegisterUserCommand, IResult<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<IResult<UserDto>> Handle(RegisterUserCommand command, CancellationToken ct)
    {
        // Check for duplicate email
        var existingUser = await _userRepository.GetByEmailAsync(command.Email, ct);
        if (existingUser != null)
        {
            return Result.Fail<UserDto>("A user with this email already exists");
            // BasedEndpoints will convert to ErrorResponseDto.Conflict
        }

        // Create user
        var user = new User
        {
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName
        };

        await _userRepository.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Return success
        return Result.Ok(user.ToDto());
        // BasedEndpoints will convert to SuccessResponseDto<UserDto>
    }
}

// Client receives:
// Success: HTTP 200
// {
//   "success": true,
//   "message": "Operation completed successfully",
//   "statusCode": 200,
//   "timestamp": "2025-12-10T10:30:00Z",
//   "traceId": "0HN4K3QJKR2T0:00000001",
//   "data": {
//     "id": "123e4567-e89b-12d3-a456-426614174000",
//     "email": "john@example.com",
//     "firstName": "John",
//     "lastName": "Doe"
//   }
// }

// Error: HTTP 409
// {
//   "success": false,
//   "message": "A user with this email already exists",
//   "statusCode": 409,
//   "timestamp": "2025-12-10T10:30:00Z",
//   "traceId": "0HN4K3QJKR2T0:00000002",
//   "errorCode": "CONFLICT",
//   "errors": ["A user with this email already exists"]
// }
```

### Example 3: Custom Endpoint with Manual Response DTOs

```csharp
// For cases where you need more control
group.MapPost("/login", async (
    LoginRequest request,
    IAuthService authService,
    HttpContext httpContext) =>
{
    var result = await authService.LoginAsync(request.Email, request.Password);
    
    if (!result.IsSuccess)
    {
        var errorResponse = ErrorResponseDto.Unauthorized(
            "Invalid email or password",
            "INVALID_CREDENTIALS"
        ).WithTraceId(httpContext);
        
        return Results.Json(errorResponse, statusCode: 401);
    }

    var successResponse = SuccessResponseDto<LoginResponse>.Ok(
        result.Value,
        "Login successful"
    ).WithTraceId(httpContext);

    return Results.Ok(successResponse);
})
.WithMetadata("Login", "User login")
.WithResponseDocs<LoginResponse>()
.AllowAnonymous();
```

### Example 4: Pagination with Response DTOs

```csharp
group.MapGet("/paginated", async (
    [AsParameters] GetUsersPaginatedQuery query,
    IHandler<GetUsersPaginatedQuery, PaginatedResult<UserDto>> handler,
    HttpContext httpContext,
    CancellationToken ct) =>
{
    var result = await handler.Handle(query, ct);
    
    var response = SuccessResponseDto<List<UserDto>>.OkWithPagination(
        result.Data,
        query.Page,
        query.PageSize,
        result.TotalCount,
        "Users retrieved successfully"
    ).WithTraceId(httpContext);

    return Results.Ok(response);
})
.WithMetadata("GetUsersPaginated", "Get paginated users")
.WithResponseDocs<List<UserDto>>();

// Client receives:
// {
//   "success": true,
//   "message": "Users retrieved successfully",
//   "statusCode": 200,
//   "timestamp": "2025-12-10T10:30:00Z",
//   "traceId": "0HN4K3QJKR2T0:00000003",
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

## Benefits

✅ **Automatic wrapping** - No need to manually create response DTOs in handlers
✅ **Consistent responses** - All endpoints return the same format
✅ **Error mapping** - FluentResults errors automatically mapped to appropriate HTTP status codes
✅ **Trace IDs** - Automatic trace ID inclusion from HttpContext
✅ **OpenAPI docs** - Easy response documentation with `.WithResponseDocs<T>()`
✅ **Type safety** - Generic support ensures compile-time checking
✅ **Minimal boilerplate** - Handlers just return `Result.Ok()` or `Result.Fail()`

## Migration Guide

### Before (Manual response creation)

```csharp
public async Task<IResult> GetUser(Guid id)
{
    var user = await _userRepository.GetByIdAsync(id);
    if (user == null)
    {
        return Results.NotFound(new { message = "User not found" });
    }
    
    return Results.Ok(user.ToDto());
}
```

### After (Automatic with BasedEndpoints)

```csharp
// Handler
public async Task<IResult<UserDto>> Handle(GetUserQuery query, CancellationToken ct)
{
    var user = await _userRepository.GetByIdAsync(query.Id, ct);
    if (user == null)
    {
        return Result.Fail<UserDto>("User not found");
    }
    
    return Result.Ok(user.ToDto());
}

// Endpoint (automatic wrapping)
group.MapGet<GetUserQuery, UserDto>("/{id:guid}")
    .WithMetadata("GetUserById", "Get user by ID")
    .WithResponseDocs<UserDto>();
```

## Summary

By updating your `BasedEndpoints` helper class to automatically wrap responses in `SuccessResponseDto` and `ErrorResponseDto`:

1. ✅ All API responses follow the same format
2. ✅ Handlers focus on business logic, not response formatting
3. ✅ Trace IDs are automatically included
4. ✅ HTTP status codes are automatically determined from error messages
5. ✅ OpenAPI documentation is simplified with `.WithResponseDocs<T>()`
6. ✅ Clients get consistent, predictable responses

This integration provides the best of both worlds: clean CQRS handlers and standardized API responses.
