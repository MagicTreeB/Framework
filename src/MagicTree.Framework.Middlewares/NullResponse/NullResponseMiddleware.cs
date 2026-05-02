using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace MagicTree.Framework.Middlewares.NullResponse;

/// <summary>
/// Middleware to remove null properties from JSON responses
/// </summary>
public class NullResponseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly NullResponseOptions _options;

    public NullResponseMiddleware(RequestDelegate next, NullResponseOptions options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Store the original response body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            // Create a new memory stream to capture the response
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Execute the next middleware in the pipeline
            await _next(context);

            // Check if response is JSON and successful (2xx)
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300 &&
                IsJsonResponse(context.Response.ContentType))
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(responseBody).ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(responseText))
                {
                    try
                    {
                        // Remove null properties from JSON
                        var cleanedJson = RemoveNullProperties(responseText);
                        
                        // Write cleaned JSON to original stream
                        var bytes = System.Text.Encoding.UTF8.GetBytes(cleanedJson);
                        await originalBodyStream.WriteAsync(bytes);
                    }
                    catch (JsonException)
                    {
                        // If JSON parsing fails, pass through original response
                        responseBody.Seek(0, SeekOrigin.Begin);
                        await responseBody.CopyToAsync(originalBodyStream);
                    }
                }
                else
                {
                    // Empty response, pass through
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
            else
            {
                // Non-JSON or non-2xx responses, pass through as-is
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        finally
        {
            // Restore the original response body stream
            context.Response.Body = originalBodyStream;
        }
    }

    private bool IsJsonResponse(string? contentType)
    {
        return contentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    private string RemoveNullProperties(string json)
    {
        // Parse JSON as JsonElement
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Process based on JSON type
        if (root.ValueKind == JsonValueKind.Object)
        {
            var cleanedObject = RemoveNullsFromObject(root);
            return JsonSerializer.Serialize(cleanedObject, _options.SerializerOptions);
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            var cleanedArray = RemoveNullsFromArray(root);
            return JsonSerializer.Serialize(cleanedArray, _options.SerializerOptions);
        }

        // Return original for primitives
        return json;
    }

    private Dictionary<string, object?> RemoveNullsFromObject(JsonElement element)
    {
        var result = new Dictionary<string, object?>();

        foreach (var property in element.EnumerateObject())
        {
            // Skip null properties based on options
            if (property.Value.ValueKind == JsonValueKind.Null && _options.RemoveNullProperties)
            {
                continue;
            }

            // Recursively process nested objects and arrays
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                result[property.Name] = RemoveNullsFromObject(property.Value);
            }
            else if (property.Value.ValueKind == JsonValueKind.Array)
            {
                result[property.Name] = RemoveNullsFromArray(property.Value);
            }
            else
            {
                result[property.Name] = ConvertJsonElement(property.Value);
            }
        }

        return result;
    }

    private List<object?> RemoveNullsFromArray(JsonElement element)
    {
        var result = new List<object?>();

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object)
            {
                result.Add(RemoveNullsFromObject(item));
            }
            else if (item.ValueKind == JsonValueKind.Array)
            {
                result.Add(RemoveNullsFromArray(item));
            }
            else
            {
                result.Add(ConvertJsonElement(item));
            }
        }

        return result;
    }

    private object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue :
                                   element.TryGetInt64(out var longValue) ? longValue :
                                   element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }
}
