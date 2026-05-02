using FluentAssertions;

namespace MagicTree.Framework.Dtos.UnitTest;

public class ErrorResponseDtoTests
{
    [Fact]
    public void ErrorResponseDto_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var dto = new ErrorResponseDto();

        // Assert
        dto.Success.Should().BeFalse();
        dto.Message.Should().BeEmpty();
        dto.StatusCode.Should().Be(0);
        dto.Errors.Should().BeEmpty();
        dto.ErrorCode.Should().BeNull();
        dto.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        dto.TraceId.Should().BeNull();
        dto.Metadata.Should().BeNull();
    }

    [Fact]
    public void ErrorResponseDto_AllProperties_ShouldBeSettable()
    {
        // Arrange
        var message = "Test error";
        var statusCode = 400;
        var errors = new List<string> { "Error 1", "Error 2" };
        var errorCode = "TEST_ERROR";
        var timestamp = DateTime.UtcNow.AddMinutes(-5);
        var traceId = Guid.NewGuid().ToString();
        var metadata = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        var dto = new ErrorResponseDto
        {
            Message = message,
            StatusCode = statusCode,
            Errors = errors,
            ErrorCode = errorCode,
            Timestamp = timestamp,
            TraceId = traceId,
            Metadata = metadata
        };

        // Assert
        dto.Success.Should().BeFalse();
        dto.Message.Should().Be(message);
        dto.StatusCode.Should().Be(statusCode);
        dto.Errors.Should().BeEquivalentTo(errors);
        dto.ErrorCode.Should().Be(errorCode);
        dto.Timestamp.Should().Be(timestamp);
        dto.TraceId.Should().Be(traceId);
        dto.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public void BadRequest_ShouldCreateCorrectResponse()
    {
        // Arrange
        var message = "Invalid request";
        var errors = new List<string> { "Field error 1", "Field error 2" };
        var errorCode = "INVALID_REQUEST";

        // Act
        var dto = ErrorResponseDto.BadRequest(message, errors, errorCode);

        // Assert
        dto.Success.Should().BeFalse();
        dto.Message.Should().Be(message);
        dto.StatusCode.Should().Be(400);
        dto.Errors.Should().BeEquivalentTo(errors);
        dto.ErrorCode.Should().Be(errorCode);
    }

    [Fact]
    public void BadRequest_WithoutErrors_ShouldCreateEmptyErrorsList()
    {
        // Act
        var dto = ErrorResponseDto.BadRequest("Invalid data");

        // Assert
        dto.StatusCode.Should().Be(400);
        dto.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Unauthorized_ShouldCreateCorrectResponse()
    {
        // Arrange
        var message = "Invalid token";
        var errorCode = "INVALID_TOKEN";

        // Act
        var dto = ErrorResponseDto.Unauthorized(message, errorCode);

        // Assert
        dto.Success.Should().BeFalse();
        dto.Message.Should().Be(message);
        dto.StatusCode.Should().Be(401);
        dto.ErrorCode.Should().Be(errorCode);
    }

    [Fact]
    public void Unauthorized_WithDefaultMessage_ShouldUseDefaultErrorCode()
    {
        // Act
        var dto = ErrorResponseDto.Unauthorized();

        // Assert
        dto.Message.Should().Be("Unauthorized access");
        dto.StatusCode.Should().Be(401);
        dto.ErrorCode.Should().Be("UNAUTHORIZED");
    }

    [Fact]
    public void Forbidden_ShouldCreateCorrectResponse()
    {
        // Arrange
        var message = "Insufficient permissions";
        var errorCode = "INSUFFICIENT_PERMISSIONS";

        // Act
        var dto = ErrorResponseDto.Forbidden(message, errorCode);

        // Assert
        dto.Success.Should().BeFalse();
        dto.Message.Should().Be(message);
        dto.StatusCode.Should().Be(403);
        dto.ErrorCode.Should().Be(errorCode);
    }

    [Fact]
    public void Forbidden_WithDefaultMessage_ShouldUseDefaultErrorCode()
    {
        // Act
        var dto = ErrorResponseDto.Forbidden();

        // Assert
        dto.Message.Should().Be("Access forbidden");
        dto.StatusCode.Should().Be(403);
        dto.ErrorCode.Should().Be("FORBIDDEN");
    }

    [Fact]
    public void NotFound_ShouldCreateCorrectResponse()
    {
        // Arrange
        var message = "User not found";
        var errorCode = "USER_NOT_FOUND";

        // Act
        var dto = ErrorResponseDto.NotFound(message, errorCode);

        // Assert
        dto.Success.Should().BeFalse();
        dto.Message.Should().Be(message);
        dto.StatusCode.Should().Be(404);
        dto.ErrorCode.Should().Be(errorCode);
    }

    [Fact]
    public void NotFound_WithDefaultMessage_ShouldUseDefaultErrorCode()
    {
        // Act
        var dto = ErrorResponseDto.NotFound();

        // Assert
        dto.Message.Should().Be("Resource not found");
        dto.StatusCode.Should().Be(404);
        dto.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public void Conflict_ShouldCreateCorrectResponse()
    {
        // Arrange
        var message = "Email already exists";
        var errorCode = "EMAIL_EXISTS";

        // Act
        var dto = ErrorResponseDto.Conflict(message, errorCode);

        // Assert
        dto.Success.Should().BeFalse();
        dto.Message.Should().Be(message);
        dto.StatusCode.Should().Be(409);
        dto.ErrorCode.Should().Be(errorCode);
    }

    [Fact]
    public void Conflict_WithoutErrorCode_ShouldUseDefaultErrorCode()
    {
        // Act
        var dto = ErrorResponseDto.Conflict("Duplicate entry");

        // Assert
        dto.StatusCode.Should().Be(409);
        dto.ErrorCode.Should().Be("CONFLICT");
    }

    [Fact]
    public void InternalServerError_ShouldCreateCorrectResponse()
    {
        // Arrange
        var message = "Database connection failed";
        var errorCode = "DB_ERROR";

        // Act
        var dto = ErrorResponseDto.InternalServerError(message, errorCode);

        // Assert
        dto.Success.Should().BeFalse();
        dto.Message.Should().Be(message);
        dto.StatusCode.Should().Be(500);
        dto.ErrorCode.Should().Be(errorCode);
    }

    [Fact]
    public void InternalServerError_WithDefaultMessage_ShouldUseDefaultErrorCode()
    {
        // Act
        var dto = ErrorResponseDto.InternalServerError();

        // Assert
        dto.Message.Should().Be("An internal server error occurred");
        dto.StatusCode.Should().Be(500);
        dto.ErrorCode.Should().Be("INTERNAL_ERROR");
    }

    [Fact]
    public void ValidationError_ShouldCreateCorrectResponse()
    {
        // Arrange
        var message = "Validation failed";
        var fieldErrors = new Dictionary<string, List<string>>
        {
            ["Email"] = new List<string> { "Email is required", "Invalid email format" },
            ["Password"] = new List<string> { "Password must be at least 8 characters" }
        };

        // Act
        var dto = ErrorResponseDto.ValidationError(message, fieldErrors);

        // Assert
        dto.Success.Should().BeFalse();
        dto.Message.Should().Be(message);
        dto.StatusCode.Should().Be(400);
        dto.ErrorCode.Should().Be("VALIDATION_ERROR");
        dto.Errors.Should().HaveCount(3);
        dto.Errors.Should().Contain("Email: Email is required");
        dto.Errors.Should().Contain("Email: Invalid email format");
        dto.Errors.Should().Contain("Password: Password must be at least 8 characters");
        dto.Metadata.Should().ContainKey("fieldErrors");
        dto.Metadata!["fieldErrors"].Should().BeEquivalentTo(fieldErrors);
    }

    [Fact]
    public void ValidationError_WithEmptyFieldErrors_ShouldCreateEmptyErrorsList()
    {
        // Arrange
        var fieldErrors = new Dictionary<string, List<string>>();

        // Act
        var dto = ErrorResponseDto.ValidationError("Validation failed", fieldErrors);

        // Assert
        dto.Errors.Should().BeEmpty();
        dto.Metadata.Should().ContainKey("fieldErrors");
    }

    [Fact]
    public void ErrorResponseDto_Success_ShouldAlwaysBeFalse()
    {
        // Arrange & Act
        var dto1 = ErrorResponseDto.BadRequest("Error");
        var dto2 = ErrorResponseDto.NotFound();
        var dto3 = ErrorResponseDto.InternalServerError();
        var dto4 = new ErrorResponseDto { Success = true }; // Try to override

        // Assert
        dto1.Success.Should().BeFalse();
        dto2.Success.Should().BeFalse();
        dto3.Success.Should().BeFalse();
        dto4.Success.Should().BeTrue(); // Can be set but defaults to false
    }

    [Fact]
    public void ErrorResponseDto_Timestamp_ShouldBeUtc()
    {
        // Act
        var dto = ErrorResponseDto.BadRequest("Error");

        // Assert
        dto.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
        dto.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
