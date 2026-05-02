using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MagicTree.Framework.Exceptions.Common;
using MagicTree.Framework.Exceptions.Middlewares;
using System.Text.Json;

namespace MagicTree.Framework.Exceptions.UnitTest.Middlewares;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<ILogger<GlobalExceptionHandler>> _loggerMock;
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly GlobalExceptionHandler _middleware;

    public GlobalExceptionHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
        _nextMock = new Mock<RequestDelegate>();
        _middleware = new GlobalExceptionHandler(_nextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_ShouldCallNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _nextMock.Setup(next => next(context)).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithEntityNotFoundException_ShouldReturn404()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var userId = Guid.NewGuid();
        var exception = new EntityNotFoundException<Guid>("User", userId);
        
        _nextMock.Setup(next => next(context)).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        context.Response.ContentType.Should().Be("application/json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseBody);
        
        response.GetProperty("error").GetString().Should().Be("USER_NOT_FOUND");
        response.GetProperty("message").GetString().Should().Contain("not found");
    }

    [Fact]
    public async Task InvokeAsync_WithEntityAlreadyExistsException_ShouldReturn409()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new EntityAlreadyExistsException("User", "Email", "test@example.com");
        
        _nextMock.Setup(next => next(context)).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseBody);
        
        response.GetProperty("error").GetString().Should().Be("USER_ALREADY_EXISTS");
    }

    [Fact]
    public async Task InvokeAsync_WithEntityValidationException_ShouldReturn400()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new EntityValidationException("User", "Email is required.");
        
        _nextMock.Setup(next => next(context)).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseBody);
        
        response.GetProperty("error").GetString().Should().Be("USER_VALIDATION_ERROR");
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidEntityOperationException_ShouldReturn400()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new InvalidEntityOperationException("User", "Activate", "AlreadyActive");
        
        _nextMock.Setup(next => next(context)).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseBody);
        
        response.GetProperty("error").GetString().Should().Be("INVALID_USER_OPERATION");
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedEntityAccessException_ShouldReturn403()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var userId = Guid.NewGuid();
        var exception = new UnauthorizedEntityAccessException("Document", userId.ToString(), "Read");
        
        _nextMock.Setup(next => next(context)).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseBody);
        
        response.GetProperty("error").GetString().Should().Be("UNAUTHORIZED_DOCUMENT_ACCESS");
    }

    [Fact]
    public async Task InvokeAsync_WithUnhandledException_ShouldReturn500()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new InvalidOperationException("Unexpected error");
        
        _nextMock.Setup(next => next(context)).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseBody);
        
        response.GetProperty("error").GetString().Should().Be("INTERNAL_SERVER_ERROR");
        response.GetProperty("message").GetString().Should().Contain("unexpected error occurred");
    }

    [Fact]
    public async Task InvokeAsync_ShouldIncludeTimestampInResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new EntityNotFoundException<Guid>("User", Guid.NewGuid());
        
        _nextMock.Setup(next => next(context)).ThrowsAsync(exception);

        // Act
        var beforeTimestamp = DateTimeOffset.UtcNow;
        await _middleware.InvokeAsync(context);
        var afterTimestamp = DateTimeOffset.UtcNow;

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseBody);
        
        var timestamp = DateTimeOffset.Parse(response.GetProperty("timestamp").GetString()!);
        timestamp.Should().BeOnOrAfter(beforeTimestamp);
        timestamp.Should().BeOnOrBefore(afterTimestamp);
    }

    [Fact]
    public async Task InvokeAsync_ShouldIncludeDetailsInResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var userId = Guid.NewGuid();
        var exception = new EntityNotFoundException<Guid>("User", userId);
        
        _nextMock.Setup(next => next(context)).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseBody);
        
        var details = response.GetProperty("details");
        details.GetProperty("EntityName").GetString().Should().Be("User");
        details.GetProperty("EntityId").GetString().Should().Be(userId.ToString());
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogWarningForDomainException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new EntityNotFoundException<Guid>("User", Guid.NewGuid());
        
        _nextMock.Setup(next => next(context)).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogErrorForUnhandledException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new InvalidOperationException("Unexpected error");
        
        _nextMock.Setup(next => next(context)).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
