using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using MMO.Core.Middlewares.NullResponse;

namespace MMO.Core.Middlewares.UnitTest.NullResponse;

public class NullResponseMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_RemovesNullProperties_FromSimpleObject()
    {
        // Arrange
        var options = new NullResponseOptions();
        var originalJson = JsonSerializer.Serialize(new { a = (string?)null, b = "b", c = true });
        var middleware = CreateMiddleware(next => async (context) =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(originalJson);
        }, options);

        var context = CreateHttpContext("GET");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        
        var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseText);
        result.Should().NotBeNull();
        result.Should().NotContainKey("a");
        result.Should().ContainKey("b");
        result.Should().ContainKey("c");
    }

    [Fact]
    public async Task InvokeAsync_RemovesNullProperties_FromNestedObject()
    {
        // Arrange
        var options = new NullResponseOptions();
        var originalJson = JsonSerializer.Serialize(new 
        { 
            a = (string?)null, 
            b = "b", 
            nested = new { x = (string?)null, y = "y" } 
        });
        var middleware = CreateMiddleware(next => async (context) =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(originalJson);
        }, options);

        var context = CreateHttpContext("GET");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        responseText.Should().NotContain("\"a\"");
        responseText.Should().NotContain("\"x\"");
        responseText.Should().Contain("\"b\"");
        responseText.Should().Contain("\"y\"");
    }

    [Fact]
    public async Task InvokeAsync_PreservesNonNullValues()
    {
        // Arrange
        var options = new NullResponseOptions();
        var originalJson = JsonSerializer.Serialize(new 
        { 
            name = "Test",
            age = 25,
            isActive = true,
            score = 99.5
        });
        var middleware = CreateMiddleware(next => async (context) =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(originalJson);
        }, options);

        var context = CreateHttpContext("GET");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        responseText.Should().Contain("name");
        responseText.Should().Contain("Test");
        responseText.Should().Contain("age");
        responseText.Should().Contain("25");
    }

    [Fact]
    public async Task InvokeAsync_HandlesArray_WithNullProperties()
    {
        // Arrange
        var options = new NullResponseOptions();
        var originalJson = JsonSerializer.Serialize(new[] 
        { 
            new { id = 1, name = (string?)null },
            new { id = 2, name = "Test" }!
        });
        var middleware = CreateMiddleware(next => async (context) =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(originalJson);
        }, options);

        var context = CreateHttpContext("GET");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        responseText.Should().Contain("\"id\"");
        responseText.Should().NotContain("\"name\":null");
    }

    [Fact]
    public async Task InvokeAsync_PassesThroughNonJsonResponse()
    {
        // Arrange
        var options = new NullResponseOptions();
        var htmlContent = "<html><body>Test</body></html>";
        var middleware = CreateMiddleware(next => async (context) =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(htmlContent);
        }, options);

        var context = CreateHttpContext("GET");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        responseText.Should().Be(htmlContent);
    }

    [Fact]
    public async Task InvokeAsync_PassesThrough_Non2xxResponse()
    {
        // Arrange
        var options = new NullResponseOptions();
        var errorJson = JsonSerializer.Serialize(new { error = "Not Found", field = (string?)null });
        var middleware = CreateMiddleware(next => async (context) =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(errorJson);
        }, options);

        var context = CreateHttpContext("GET");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        // Error responses should pass through unchanged
        responseText.Should().Contain("\"error\"");
    }

    [Fact]
    public async Task InvokeAsync_HandlesEmptyObject()
    {
        // Arrange
        var options = new NullResponseOptions();
        var emptyJson = "{}";
        var middleware = CreateMiddleware(next => async (context) =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(emptyJson);
        }, options);

        var context = CreateHttpContext("GET");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        responseText.Should().Be("{}");
    }

    [Fact]
    public async Task InvokeAsync_ConfigurableRemoveNullProperties_False()
    {
        // Arrange
        var options = new NullResponseOptions { RemoveNullProperties = false };
        var originalJson = JsonSerializer.Serialize(new { a = (string?)null, b = "b" });
        var middleware = CreateMiddleware(next => async (context) =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(originalJson);
        }, options);

        var context = CreateHttpContext("GET");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        responseText.Should().Contain("\"a\"");
    }

    [Fact]
    public async Task InvokeAsync_HandlesComplexNestedStructure()
    {
        // Arrange
        var options = new NullResponseOptions();
        var complexObject = new
        {
            id = 1,
            nullField = (string?)null,
            nested = new
            {
                value = "test",
                nullNested = (string?)null,
                deepNested = new
                {
                    data = "data",
                    nullDeep = (string?)null
                }
            },
            arrayField = new[]
            {
                new { item = "a", nullItem = (string?)null },
                new { item = "b", nullItem = (string?)null }
            }
        };
        var originalJson = JsonSerializer.Serialize(complexObject);
        var middleware = CreateMiddleware(next => async (context) =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(originalJson);
        }, options);

        var context = CreateHttpContext("GET");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        responseText.Should().NotContain("nullField");
        responseText.Should().NotContain("nullNested");
        responseText.Should().NotContain("nullDeep");
        responseText.Should().NotContain("nullItem");
        responseText.Should().Contain("\"id\"");
        responseText.Should().Contain("\"value\"");
        responseText.Should().Contain("\"data\"");
    }

    private NullResponseMiddleware CreateMiddleware(Func<RequestDelegate, RequestDelegate> nextFactory, NullResponseOptions options)
    {
        RequestDelegate next = nextFactory(async (context) => await Task.CompletedTask);
        return new NullResponseMiddleware(next, options);
    }

    private HttpContext CreateHttpContext(string method)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Response.Body = new MemoryStream();
        return context;
    }
}
