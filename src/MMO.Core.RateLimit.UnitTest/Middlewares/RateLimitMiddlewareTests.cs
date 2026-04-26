using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MMO.Core.RateLimit.Interfaces;
using MMO.Core.RateLimit.Middlewares;
using MMO.Core.RateLimit.Models;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace MMO.Core.RateLimit.UnitTest.Middlewares;

public class RateLimitMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenDisabled_ShouldNotRateLimit()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new MMO.Core.RateLimit.Options.RateLimitOptions { Enabled = false });
        var serviceMock = new Mock<IRateLimitService>();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new RateLimitMiddleware(next, options, serviceMock.Object);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        serviceMock.Verify(x => x.CheckRateLimitAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<int>(), 
            It.IsAny<int>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhitelistedIP_ShouldBypassRateLimit()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new MMO.Core.RateLimit.Options.RateLimitOptions 
        { 
            Enabled = true,
            IpWhitelist = new List<string> { "ip:127.0.0.1" } // Must include "ip:" prefix
        });
        var serviceMock = new Mock<IRateLimitService>();
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new RateLimitMiddleware(next, options, serviceMock.Object);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        serviceMock.Verify(x => x.CheckRateLimitAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<int>(), 
            It.IsAny<int>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_UnderLimit_ShouldAllow()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new MMO.Core.RateLimit.Options.RateLimitOptions 
        { 
            Enabled = true,
            Global = new MMO.Core.RateLimit.Options.RateLimitRule { Limit = 10, WindowSeconds = 60 }
        });
        
        var serviceMock = new Mock<IRateLimitService>();
        serviceMock.Setup(x => x.CheckRateLimitAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<int>(), 
            It.IsAny<int>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RateLimitResult
            {
                IsAllowed = true,
                CurrentCount = 5,
                Limit = 10,
                ResetAt = DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeSeconds(),
                RetryAfterSeconds = 30
            });

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new RateLimitMiddleware(next, options, serviceMock.Object);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
        context.Request.Path = "/api/test";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.OK);
    }

    [Fact]
    public async Task InvokeAsync_ExceedsLimit_ShouldReturn429()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new MMO.Core.RateLimit.Options.RateLimitOptions 
        { 
            Enabled = true,
            Global = new MMO.Core.RateLimit.Options.RateLimitRule { Limit = 10, WindowSeconds = 60 }
        });
        
        var serviceMock = new Mock<IRateLimitService>();
        serviceMock.Setup(x => x.CheckRateLimitAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<int>(), 
            It.IsAny<int>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RateLimitResult
            {
                IsAllowed = false,
                CurrentCount = 11,
                Limit = 10,
                ResetAt = DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeSeconds(),
                RetryAfterSeconds = 30
            });

        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new RateLimitMiddleware(next, options, serviceMock.Object);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.TooManyRequests);
        context.Response.ContentType.Should().Be("application/json");
        
        // Read response body
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        var response = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseBody);
        
        response.Should().ContainKey("error");
        response!["error"].GetString().Should().Be("Rate limit exceeded");
    }

    [Fact]
    public async Task InvokeAsync_ShouldAddRateLimitHeaders()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new MMO.Core.RateLimit.Options.RateLimitOptions 
        { 
            Enabled = true,
            Global = new MMO.Core.RateLimit.Options.RateLimitRule { Limit = 10, WindowSeconds = 60 },
            Headers = new MMO.Core.RateLimit.Options.RateLimitHeaders { IncludeHeaders = true }
        });
        
        var serviceMock = new Mock<IRateLimitService>();
        serviceMock.Setup(x => x.CheckRateLimitAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<int>(), 
            It.IsAny<int>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RateLimitResult
            {
                IsAllowed = true,
                CurrentCount = 5,
                Limit = 10,
                ResetAt = DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeSeconds(),
                RetryAfterSeconds = 30
            });

        RequestDelegate next = _ => Task.CompletedTask;

        var middleware = new RateLimitMiddleware(next, options, serviceMock.Object);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
        context.Request.Path = "/api/test";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("X-RateLimit-Limit");
        context.Response.Headers.Should().ContainKey("X-RateLimit-Remaining");
        context.Response.Headers.Should().ContainKey("X-RateLimit-Reset");
        context.Response.Headers["X-RateLimit-Limit"].ToString().Should().Be("10");
        context.Response.Headers["X-RateLimit-Remaining"].ToString().Should().Be("5");
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedUser_ShouldUseUserId()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new MMO.Core.RateLimit.Options.RateLimitOptions 
        { 
            Enabled = true,
            Global = new MMO.Core.RateLimit.Options.RateLimitRule { Limit = 10, WindowSeconds = 60 }
        });
        
        string? capturedIdentifier = null;
        var serviceMock = new Mock<IRateLimitService>();
        serviceMock.Setup(x => x.CheckRateLimitAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<int>(), 
            It.IsAny<int>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RateLimitResult
            {
                IsAllowed = true,
                CurrentCount = 1,
                Limit = 10,
                ResetAt = DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeSeconds(),
                RetryAfterSeconds = 60
            })
            .Callback<string, string, int, int, CancellationToken>((id, _, _, _, _) => capturedIdentifier = id);

        RequestDelegate next = _ => Task.CompletedTask;

        var middleware = new RateLimitMiddleware(next, options, serviceMock.Object);
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "john.doe")
        }));
        context.Request.Path = "/api/test";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        capturedIdentifier.Should().Be("user:john.doe");
    }

    [Fact]
    public async Task InvokeAsync_EndpointSpecificRule_ShouldUseEndpointLimit()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new MMO.Core.RateLimit.Options.RateLimitOptions 
        { 
            Enabled = true,
            Global = new MMO.Core.RateLimit.Options.RateLimitRule { Limit = 100, WindowSeconds = 60 },
            Endpoints = new Dictionary<string, MMO.Core.RateLimit.Options.RateLimitRule>
            {
                { "/api/login", new MMO.Core.RateLimit.Options.RateLimitRule { Limit = 5, WindowSeconds = 300 } }
            }
        });
        
        int capturedLimit = 0;
        int capturedWindow = 0;
        var serviceMock = new Mock<IRateLimitService>();
        serviceMock.Setup(x => x.CheckRateLimitAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<int>(), 
            It.IsAny<int>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RateLimitResult
            {
                IsAllowed = true,
                CurrentCount = 1,
                Limit = 5,
                ResetAt = DateTimeOffset.UtcNow.AddSeconds(300).ToUnixTimeSeconds(),
                RetryAfterSeconds = 300
            })
            .Callback<string, string, int, int, CancellationToken>((_, _, limit, window, _) => 
            {
                capturedLimit = limit;
                capturedWindow = window;
            });

        RequestDelegate next = _ => Task.CompletedTask;

        var middleware = new RateLimitMiddleware(next, options, serviceMock.Object);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
        context.Request.Path = "/api/login";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        capturedLimit.Should().Be(5);
        capturedWindow.Should().Be(300);
    }
}
