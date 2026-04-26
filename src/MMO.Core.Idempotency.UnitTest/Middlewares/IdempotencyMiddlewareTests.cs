using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MMO.Core.Idempotency.Interfaces;
using MMO.Core.Idempotency.Middlewares;
using MMO.Core.Idempotency.Models;
using System.Text.Json;

namespace MMO.Core.Idempotency.UnitTest.Middlewares;

public class IdempotencyMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenDisabled_ShouldNotCheckIdempotency()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new MMO.Core.Idempotency.Options.IdempotencyOptions 
        { 
            Enabled = false 
        });
        var storageMock = new Mock<IIdempotencyStorage>();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new IdempotencyMiddleware(next, options, storageMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Headers["X-Idempotency-Key"] = Guid.NewGuid().ToString();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        storageMock.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_GetRequest_ShouldSkipIdempotency()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new MMO.Core.Idempotency.Options.IdempotencyOptions 
        { 
            Enabled = true 
        });
        var storageMock = new Mock<IIdempotencyStorage>();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new IdempotencyMiddleware(next, options, storageMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        storageMock.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_NoIdempotencyKey_ShouldProceed()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new MMO.Core.Idempotency.Options.IdempotencyOptions 
        { 
            Enabled = true 
        });
        var storageMock = new Mock<IIdempotencyStorage>();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new IdempotencyMiddleware(next, options, storageMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        storageMock.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_InvalidIdempotencyKey_ShouldReturn400()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new MMO.Core.Idempotency.Options.IdempotencyOptions 
        { 
            Enabled = true 
        });
        var storageMock = new Mock<IIdempotencyStorage>();
        RequestDelegate next = _ => Task.CompletedTask;

        var middleware = new IdempotencyMiddleware(next, options, storageMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Headers["X-Idempotency-Key"] = "invalid-not-a-guid";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.ContentType.Should().Be("application/json");
        
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        var response = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseBody);
        
        response.Should().ContainKey("error");
        response!["error"].GetString().Should().Be("Invalid idempotency key");
    }

    [Fact]
    public async Task InvokeAsync_RequestInProgress_ShouldReturn409()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new MMO.Core.Idempotency.Options.IdempotencyOptions 
        { 
            Enabled = true 
        });
        var storageMock = new Mock<IIdempotencyStorage>();
        var processingRecord = new IdempotencyRecord
        {
            Key = "test-key",
            IsProcessing = true
        };
        storageMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(processingRecord);

        RequestDelegate next = _ => Task.CompletedTask;

        var middleware = new IdempotencyMiddleware(next, options, storageMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Headers["X-Idempotency-Key"] = Guid.NewGuid().ToString();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        context.Response.ContentType.Should().Be("application/json");
        
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        var response = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseBody);
        
        response.Should().ContainKey("error");
        response!["error"].GetString().Should().Be("Request in progress");
    }

    [Fact]
    public async Task InvokeAsync_CachedResponse_ShouldReplayResponse()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new MMO.Core.Idempotency.Options.IdempotencyOptions 
        { 
            Enabled = true,
            IncludeTimestampHeader = true
        });
        var storageMock = new Mock<IIdempotencyStorage>();
        var cachedRecord = new IdempotencyRecord
        {
            Key = "cached-key",
            IsProcessing = false,
            StatusCode = 201,
            ResponseBody = "{\"id\":123,\"name\":\"Test\"}",
            ContentType = "application/json",
            Headers = new Dictionary<string, string> 
            { 
                { "X-Custom-Header", "CustomValue" } 
            },
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        storageMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedRecord);

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new IdempotencyMiddleware(next, options, storageMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Headers["X-Idempotency-Key"] = Guid.NewGuid().ToString();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(201);
        context.Response.ContentType.Should().Be("application/json");
        context.Response.Headers.Should().ContainKey("X-Custom-Header");
        context.Response.Headers.Should().ContainKey("X-Idempotency-Replayed-At");
    }

    [Fact]
    public async Task InvokeAsync_NewRequest_ShouldProcessAndCache()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new MMO.Core.Idempotency.Options.IdempotencyOptions 
        { 
            Enabled = true,
            ExpirationHours = 24
        });
        var storageMock = new Mock<IIdempotencyStorage>();
        storageMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyRecord?)null);
        storageMock.Setup(x => x.TryMarkAsProcessingAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<int>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var nextCalled = false;
        RequestDelegate next = async ctx =>
        {
            nextCalled = true;
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("{\"success\":true}");
        };

        var middleware = new IdempotencyMiddleware(next, options, storageMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/test";
        context.Request.Headers["X-Idempotency-Key"] = Guid.NewGuid().ToString();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
        storageMock.Verify(x => x.SetAsync(
            It.Is<IdempotencyRecord>(r => 
                r.StatusCode == 200 && 
                !r.IsProcessing &&
                r.ResponseBody != null), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_EndpointFiltering_ShouldOnlyApplyToConfiguredEndpoints()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new MMO.Core.Idempotency.Options.IdempotencyOptions 
        { 
            Enabled = true,
            Endpoints = new List<string> { "/api/orders", "/api/payments/*" }
        });
        var storageMock = new Mock<IIdempotencyStorage>();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new IdempotencyMiddleware(next, options, storageMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/users"; // Not in configured endpoints
        context.Request.Headers["X-Idempotency-Key"] = Guid.NewGuid().ToString();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        storageMock.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WildcardEndpoint_ShouldMatch()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new MMO.Core.Idempotency.Options.IdempotencyOptions 
        { 
            Enabled = true,
            Endpoints = new List<string> { "/api/payments/*" }
        });
        var storageMock = new Mock<IIdempotencyStorage>();
        storageMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdempotencyRecord?)null);
        storageMock.Setup(x => x.TryMarkAsProcessingAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<int>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        RequestDelegate next = _ => Task.CompletedTask;

        var middleware = new IdempotencyMiddleware(next, options, storageMock.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/payments/create";
        context.Request.Headers["X-Idempotency-Key"] = Guid.NewGuid().ToString();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        storageMock.Verify(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
