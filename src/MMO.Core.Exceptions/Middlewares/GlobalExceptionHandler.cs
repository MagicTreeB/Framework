using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MMO.Core.Exceptions.Base;
using MMO.Core.Exceptions.Common;
using System.Text.Json;

namespace MMO.Core.Exceptions.Middlewares;

/// <summary>
/// Global exception handler middleware for catching and mapping domain exceptions to HTTP responses.
/// </summary>
public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception occurred: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
            await HandleDomainExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleUnhandledExceptionAsync(context, ex);
        }
    }

    private static async Task HandleDomainExceptionAsync(HttpContext context, DomainException exception)
    {
        // Check if response has already started - if so, we can't modify headers
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            EntityNotFoundException<Guid> => StatusCodes.Status404NotFound,
            EntityNotFoundException<int> => StatusCodes.Status404NotFound,
            EntityNotFoundException<string> => StatusCodes.Status404NotFound,
            EntityAlreadyExistsException => StatusCodes.Status409Conflict,
            EntityValidationException => StatusCodes.Status400BadRequest,
            InvalidEntityOperationException => StatusCodes.Status400BadRequest,
            UnauthorizedEntityAccessException => StatusCodes.Status403Forbidden,
            _ => exception.ErrorCode.Contains("NOT_FOUND") ? StatusCodes.Status404NotFound :
                 exception.ErrorCode.Contains("ALREADY_EXISTS") ? StatusCodes.Status409Conflict :
                 exception.ErrorCode.Contains("UNAUTHORIZED") ? StatusCodes.Status403Forbidden :
                 StatusCodes.Status400BadRequest
        };

        var response = new
        {
            error = exception.ErrorCode,
            message = exception.Message,
            details = exception.Details,
            timestamp = DateTimeOffset.UtcNow
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static async Task HandleUnhandledExceptionAsync(HttpContext context, Exception exception)
    {
        // Check if response has already started - if so, we can't modify headers
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new
        {
            error = "INTERNAL_SERVER_ERROR",
            message = "An unexpected error occurred. Please try again later.",
            timestamp = DateTimeOffset.UtcNow
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}
