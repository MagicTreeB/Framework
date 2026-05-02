using DKNet.SlimBus.Extensions;
using FluentResults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MagicTree.Framework.Extensions;

/// <summary>
/// Base class providing helper methods for defining API endpoints with CQRS pattern using DKNet.SlimBus
/// </summary>
public static class BasedEndpoints
{
    /// <summary>
    /// Maps a POST endpoint that sends a command via SlimBus
    /// </summary>
    public static RouteHandlerBuilder MapCqrsPost<TCommand, TResponse>(
        this RouteGroupBuilder group,
        string pattern)
        where TCommand : Fluents.Requests.IWitResponse<TResponse>
    {
        return group.MapPost(pattern, async (
                [FromBody] TCommand command,
                [FromServices] Fluents.Requests.IHandler<TCommand, TResponse> handler,
                CancellationToken ct) =>
            {
                var result = await handler.OnHandle(command, ct);
                return ConvertToHttpResult(result);
            })
            // These are the critical parts for OpenAPI/Scalar payload generation:
            .Accepts<TCommand>("application/json")
            .Produces<TResponse>(StatusCodes.Status200OK);
    }

    /// <summary>
    /// Maps a GET endpoint that sends a query via SlimBus
    /// </summary>
    public static RouteHandlerBuilder MapCqrsGet<TQuery, TResponse>(
        this RouteGroupBuilder group,
        string pattern)
        where TQuery : Fluents.Queries.IWitResponse<TResponse>, new()
    {
        return group.MapGet(pattern, async (
                HttpContext httpContext,
                [FromServices] Fluents.Queries.IHandler<TQuery, TResponse> handler,
                CancellationToken ct) =>
            {
                try
                {
                    // Create query instance and bind route/query parameters
                    var query = new TQuery();
                    
                    // Bind route values to query properties
                    foreach (var routeValue in httpContext.Request.RouteValues)
                    {
                        var property = typeof(TQuery).GetProperty(routeValue.Key, 
                            System.Reflection.BindingFlags.Public | 
                            System.Reflection.BindingFlags.Instance | 
                            System.Reflection.BindingFlags.IgnoreCase);
                        
                        if (property != null && routeValue.Value != null)
                        {
                            try
                            {
                                object? convertedValue;
                                if (property.PropertyType == typeof(Guid))
                                {
                                    convertedValue = Guid.Parse(routeValue.Value.ToString()!);
                                }
                                else
                                {
                                    convertedValue = Convert.ChangeType(routeValue.Value, property.PropertyType);
                                }
                                property.SetValue(query, convertedValue);
                            }
                            catch
                            {
                                // Skip if conversion fails
                            }
                        }
                    }
                    
                    // Bind query string parameters
                    foreach (var queryParam in httpContext.Request.Query)
                    {
                        var property = typeof(TQuery).GetProperty(queryParam.Key, 
                            System.Reflection.BindingFlags.Public | 
                            System.Reflection.BindingFlags.Instance | 
                            System.Reflection.BindingFlags.IgnoreCase);
                        
                        if (property != null)
                        {
                            var value = queryParam.Value.ToString();
                            if (!string.IsNullOrEmpty(value))
                            {
                                try
                                {
                                    var convertedValue = Convert.ChangeType(value, property.PropertyType);
                                    property.SetValue(query, convertedValue);
                                }
                                catch
                                {
                                    // Skip if conversion fails
                                }
                            }
                        }
                    }
                    
                    var result = await handler.OnHandle(query, ct);
                    return result == null ? Results.NotFound() : Results.Ok(result);
                }
                catch (Exception ex)
                {
                    // Log the exception details for debugging
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error");
                }
            })
            .Produces<TResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Maps a PUT endpoint that sends a command via SlimBus
    /// </summary>
    public static RouteHandlerBuilder MapCqrsPut<TCommand, TResponse>(
        this RouteGroupBuilder group,
        string pattern)
        where TCommand : Fluents.Requests.IWitResponse<TResponse>
    {
        return group.MapPut(pattern, async (
                Guid id,
                [FromBody] TCommand command,
                [FromServices] Fluents.Requests.IHandler<TCommand, TResponse> handler,
                CancellationToken ct) =>
            {
                // Set the Id property from route parameter if the command has an Id property
                var idProperty = typeof(TCommand).GetProperty("Id");
                if (idProperty != null && idProperty.PropertyType == typeof(Guid))
                {
                    idProperty.SetValue(command, id);
                }
                
                var result = await handler.OnHandle(command, ct);
                return ConvertToHttpResult(result);
            })
            .Accepts<TCommand>("application/json")
            .Produces<TResponse>(StatusCodes.Status200OK);
    }

    /// <summary>
    /// Maps a DELETE endpoint that sends a command via SlimBus
    /// </summary>
    public static RouteHandlerBuilder MapCqrsDelete<TCommand, TResponse>(
        this RouteGroupBuilder group,
        string pattern)
        where TCommand : Fluents.Requests.IWitResponse<TResponse>, new()
    {
        return group.MapDelete(pattern, async (
                [AsParameters] TCommand command,
                [FromServices] Fluents.Requests.IHandler<TCommand, TResponse> handler,
                CancellationToken ct) =>
            {
                var result = await handler.OnHandle(command, ct);
                return ConvertToHttpResult(result);
            })
            .Produces<TResponse>(StatusCodes.Status200OK);
    }

    /// <summary>
    /// Maps a PATCH endpoint that sends a command via SlimBus
    /// </summary>
    public static RouteHandlerBuilder MapCqrsPatch<TCommand, TResponse>(
        this RouteGroupBuilder group,
        string pattern)
        where TCommand : Fluents.Requests.IWitResponse<TResponse>
    {
        return group.MapPatch(pattern, async (
                [FromBody] TCommand command,
                [FromServices] Fluents.Requests.IHandler<TCommand, TResponse> handler,
                CancellationToken ct) =>
            {
                var result = await handler.OnHandle(command, ct);
                return ConvertToHttpResult(result);
            })
            .Accepts<TCommand>("application/json")
            .Produces<TResponse>(StatusCodes.Status200OK);
    }

    /// <summary>
    /// Creates a route group with common configuration
    /// </summary>
    public static RouteGroupBuilder CreateGroup(
        this IEndpointRouteBuilder app,
        string prefix,
        string tag)
    {
        return app.MapGroup(prefix)
            .WithTags(tag);
    }

    /// <summary>
    /// Adds common metadata to an endpoint
    /// </summary>
    public static RouteHandlerBuilder WithMetadata(
        this RouteHandlerBuilder builder,
        string name,
        string summary,
        string? description = null)
    {
        builder.WithName(name).WithSummary(summary);
        
        if (!string.IsNullOrEmpty(description))
        {
            builder.WithDescription(description);
        }

        return builder;
    }

    /// <summary>
    /// Adds a request body example to the OpenAPI documentation (auto-generated from TCommand)
    /// </summary>
#pragma warning disable ASPDEPR002 // WithOpenApi is deprecated but needed for example customization
    public static RouteHandlerBuilder WithRequestExample<TCommand>(
        this RouteHandlerBuilder builder)
        where TCommand : new()
    {
        return builder.WithOpenApi(op =>
        {
            if (op.RequestBody?.Content is null) return op;

            if (op.RequestBody.Content.TryGetValue("application/json", out var mediaType))
            {
                // Auto-generate example from TCommand type
                var example = new TCommand();
                var jsonString = JsonSerializer.Serialize(example, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var jsonNode = JsonNode.Parse(jsonString);
                mediaType.Example = jsonNode;
            }
            return op;
        });
    }

    /// <summary>
    /// Adds a request body example to the OpenAPI documentation (custom JSON)
    /// </summary>
    public static RouteHandlerBuilder WithRequestExample<TCommand>(
        this RouteHandlerBuilder builder,
        string jsonExample)
    {
        return builder.WithOpenApi(op =>
        {
            if (op.RequestBody?.Content is null) return op;

            if (op.RequestBody.Content.TryGetValue("application/json", out var mediaType))
            {
                var jsonNode = JsonNode.Parse(jsonExample);
                mediaType.Example = jsonNode;
            }
            return op;
        });
    }
#pragma warning restore ASPDEPR002

    /// <summary>
    /// Converts a FluentResults.IResult to an IResult (HTTP result)
    /// </summary>
    private static IResult ConvertToHttpResult<T>(IResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        // Return bad request with error messages
        var errors = result.Errors.Select(e => e.Message).ToList();
        return Results.BadRequest(new { errors });
    }
}
