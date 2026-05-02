using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MagicTree.Framework.Idempotency.Interfaces;
using MagicTree.Framework.Idempotency.Models;
using MagicTree.Framework.Idempotency.Options;
using System.Text;
using System.Text.Json;

namespace MagicTree.Framework.Idempotency.Middlewares;

/// <summary>
/// Middleware for handling idempotency keys to prevent duplicate operations
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IdempotencyOptions _options;
    private readonly IIdempotencyStorage _storage;

    public IdempotencyMiddleware(
        RequestDelegate next,
        IOptions<IdempotencyOptions> options,
        IIdempotencyStorage storage)
    {
        _next = next;
        _options = options.Value;
        _storage = storage;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip if idempotency is disabled
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        // Check if request method requires idempotency
        if (!_options.HttpMethods.Contains(context.Request.Method.ToUpper()))
        {
            await _next(context);
            return;
        }

        // Check if endpoint matches configured endpoints (if any)
        if (_options.Endpoints.Any() && !MatchesEndpoint(context.Request.Path.Value ?? "/"))
        {
            await _next(context);
            return;
        }

        // Get idempotency key from header
        if (!context.Request.Headers.TryGetValue(_options.HeaderName, out var idempotencyKey) ||
            string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _next(context);
            return;
        }

        var key = idempotencyKey.ToString();

        // Validate key format (should be GUID)
        if (!Guid.TryParse(key, out _))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "Invalid idempotency key",
                message = "Idempotency key must be a valid GUID"
            }));
            return;
        }

        // Check if request was already processed
        var existingRecord = await _storage.GetAsync(key, context.RequestAborted);
        if (existingRecord != null)
        {
            // If still processing, return 409 Conflict
            if (existingRecord.IsProcessing)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "Request in progress",
                    message = "A request with this idempotency key is currently being processed"
                }));
                return;
            }

            // Return cached response
            await ReplayCachedResponse(context, existingRecord);
            return;
        }

        // Mark as processing
        var marked = await _storage.TryMarkAsProcessingAsync(
            key,
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            _options.ExpirationHours,
            context.RequestAborted);

        if (!marked)
        {
            // Another request beat us to it
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "Request in progress",
                message = "A request with this idempotency key is currently being processed"
            }));
            return;
        }

        try
        {
            // Capture response
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // Only cache successful responses (2xx)
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                // Read response body
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();
                responseBody.Seek(0, SeekOrigin.Begin);

                // Store idempotency record
                var record = new IdempotencyRecord
                {
                    Key = key,
                    StatusCode = context.Response.StatusCode,
                    Headers = context.Response.Headers
                        .Where(h => !h.Key.StartsWith("Transfer-", StringComparison.OrdinalIgnoreCase))
                        .ToDictionary(h => h.Key, h => h.Value.ToString()),
                    ResponseBody = responseBodyText,
                    ContentType = context.Response.ContentType,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(_options.ExpirationHours),
                    IsProcessing = false,
                    RequestMethod = context.Request.Method,
                    RequestPath = context.Request.Path.Value ?? "/"
                };

                await _storage.SetAsync(record, context.RequestAborted);
            }
            else
            {
                // Request failed, remove processing mark
                await _storage.RemoveProcessingMarkAsync(key, context.RequestAborted);
            }

            // Copy response back to original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch
        {
            // On exception, remove processing mark
            await _storage.RemoveProcessingMarkAsync(key, context.RequestAborted);
            throw;
        }
    }

    private async Task ReplayCachedResponse(HttpContext context, IdempotencyRecord record)
    {
        context.Response.StatusCode = record.StatusCode;

        // Set headers
        foreach (var header in record.Headers)
        {
            context.Response.Headers[header.Key] = header.Value;
        }

        // Set content type
        if (!string.IsNullOrEmpty(record.ContentType))
        {
            context.Response.ContentType = record.ContentType;
        }

        // Add timestamp header
        if (_options.IncludeTimestampHeader)
        {
            context.Response.Headers[_options.TimestampHeaderName] = record.CreatedAt.ToString("O");
        }

        // Write response body
        if (!string.IsNullOrEmpty(record.ResponseBody))
        {
            await context.Response.WriteAsync(record.ResponseBody);
        }
    }

    private bool MatchesEndpoint(string path)
    {
        foreach (var endpoint in _options.Endpoints)
        {
            // Exact match
            if (endpoint.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Wildcard match
            if (endpoint.EndsWith("/*"))
            {
                var prefix = endpoint[..^2];
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
