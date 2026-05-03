using FluentAssertions;

namespace MagicTree.Framework.Dtos.UnitTest;

public class SuccessResponseDtoTests
{
    [Fact]
    public void SuccessResponseDto_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var dto = new SuccessResponseDto();

        // Assert
        dto.Success.Should().BeTrue();
        dto.Message.Should().BeEmpty();
        dto.StatusCode.Should().Be((StatusCode)200);
        dto.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        dto.TraceId.Should().BeNull();
        dto.Metadata.Should().BeNull();
    }

    [Fact]
    public void SuccessResponseDto_AllProperties_ShouldBeSettable()
    {
        // Arrange
        var message = "Success";
        var statusCode = (StatusCode)201;
        var timestamp = DateTime.UtcNow.AddMinutes(-5);
        var traceId = Guid.NewGuid().ToString();
        var metadata = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        var dto = new SuccessResponseDto
        {
            Message = message,
            StatusCode = statusCode,
            Timestamp = timestamp,
            TraceId = traceId,
            Metadata = metadata
        };

        // Assert
        dto.Success.Should().BeTrue();
        dto.Message.Should().Be(message);
        dto.StatusCode.Should().Be(statusCode);
        dto.Timestamp.Should().Be(timestamp);
        dto.TraceId.Should().Be(traceId);
        dto.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public void Ok_ShouldCreateCorrectResponse()
    {
        // Arrange
        var message = "Operation successful";

        // Act
        var dto = SuccessResponseDto.Ok(message);

        // Assert
        dto.Success.Should().BeTrue();
        dto.Message.Should().Be(message);
        dto.StatusCode.Should().Be((StatusCode)200);
    }

    [Fact]
    public void Ok_WithDefaultMessage_ShouldUseDefaultMessage()
    {
        // Act
        var dto = SuccessResponseDto.Ok();

        // Assert
        dto.Message.Should().Be("Operation completed successfully");
        dto.StatusCode.Should().Be((StatusCode)200);
    }

    [Fact]
    public void Created_ShouldCreateCorrectResponse()
    {
        // Arrange
        var message = "User created";

        // Act
        var dto = SuccessResponseDto.Created(message);

        // Assert
        dto.Success.Should().BeTrue();
        dto.Message.Should().Be(message);
        dto.StatusCode.Should().Be((StatusCode)201);
    }

    [Fact]
    public void Created_WithDefaultMessage_ShouldUseDefaultMessage()
    {
        // Act
        var dto = SuccessResponseDto.Created();

        // Assert
        dto.Message.Should().Be("Resource created successfully");
        dto.StatusCode.Should().Be((StatusCode)201);
    }

    [Fact]
    public void Accepted_ShouldCreateCorrectResponse()
    {
        // Arrange
        var message = "Job queued";

        // Act
        var dto = SuccessResponseDto.Accepted(message);

        // Assert
        dto.Success.Should().BeTrue();
        dto.Message.Should().Be(message);
        dto.StatusCode.Should().Be((StatusCode)202);
    }

    [Fact]
    public void Accepted_WithDefaultMessage_ShouldUseDefaultMessage()
    {
        // Act
        var dto = SuccessResponseDto.Accepted();

        // Assert
        dto.Message.Should().Be("Request accepted for processing");
        dto.StatusCode.Should().Be((StatusCode)202);
    }

    [Fact]
    public void SuccessResponseDto_Timestamp_ShouldBeUtc()
    {
        // Act
        var dto = SuccessResponseDto.Ok();

        // Assert
        dto.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
        dto.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    // Tests for generic SuccessResponseDto<T>
    [Fact]
    public void GenericSuccessResponseDto_ShouldHaveDataProperty()
    {
        // Arrange
        var data = new { Id = 1, Name = "Test" };

        // Act
        var dto = new SuccessResponseDto<object>
        {
            Data = data,
            Message = "Success"
        };

        // Assert
        dto.Data.Should().BeEquivalentTo(data);
        dto.Success.Should().BeTrue();
    }

    [Fact]
    public void GenericOk_ShouldCreateCorrectResponse()
    {
        // Arrange
        var data = new { Id = 123, Name = "Test User" };
        var message = "User retrieved";

        // Act
        var dto = SuccessResponseDto<object>.Ok(data, message);

        // Assert
        dto.Success.Should().BeTrue();
        dto.Data.Should().BeEquivalentTo(data);
        dto.Message.Should().Be(message);
        dto.StatusCode.Should().Be((StatusCode)200);
    }

    [Fact]
    public void GenericOk_WithDefaultMessage_ShouldUseDefaultMessage()
    {
        // Arrange
        var data = "test data";

        // Act
        var dto = SuccessResponseDto<string>.Ok(data);

        // Assert
        dto.Data.Should().Be(data);
        dto.Message.Should().Be("Operation completed successfully");
        dto.StatusCode.Should().Be((StatusCode)200);
    }

    [Fact]
    public void GenericCreated_ShouldCreateCorrectResponse()
    {
        // Arrange
        var data = new { Id = 456, Email = "test@example.com" };
        var message = "Account created";

        // Act
        var dto = SuccessResponseDto<object>.Created(data, message);

        // Assert
        dto.Success.Should().BeTrue();
        dto.Data.Should().BeEquivalentTo(data);
        dto.Message.Should().Be(message);
        dto.StatusCode.Should().Be((StatusCode)201);
    }

    [Fact]
    public void GenericCreated_WithDefaultMessage_ShouldUseDefaultMessage()
    {
        // Arrange
        var data = 789;

        // Act
        var dto = SuccessResponseDto<int>.Created(data);

        // Assert
        dto.Data.Should().Be(data);
        dto.Message.Should().Be("Resource created successfully");
        dto.StatusCode.Should().Be((StatusCode)201);
    }

    [Fact]
    public void GenericAccepted_ShouldCreateCorrectResponse()
    {
        // Arrange
        var data = new { JobId = "job-123" };
        var message = "Processing started";

        // Act
        var dto = SuccessResponseDto<object>.Accepted(data, message);

        // Assert
        dto.Success.Should().BeTrue();
        dto.Data.Should().BeEquivalentTo(data);
        dto.Message.Should().Be(message);
        dto.StatusCode.Should().Be((StatusCode)202);
    }

    [Fact]
    public void GenericAccepted_WithDefaultMessage_ShouldUseDefaultMessage()
    {
        // Arrange
        var data = true;

        // Act
        var dto = SuccessResponseDto<bool>.Accepted(data);

        // Assert
        dto.Data.Should().Be(data);
        dto.Message.Should().Be("Request accepted for processing");
        dto.StatusCode.Should().Be((StatusCode)202);
    }

    [Fact]
    public void OkWithPagination_ShouldCreateCorrectResponse()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3, 4, 5 };
        var page = 2;
        var pageSize = 5;
        var totalCount = 23;
        var message = "Users retrieved";

        // Act
        var dto = SuccessResponseDto<List<int>>.OkWithPagination(data, page, pageSize, totalCount, message);

        // Assert
        dto.Success.Should().BeTrue();
        dto.Data.Should().BeEquivalentTo(data);
        dto.Message.Should().Be(message);
        dto.StatusCode.Should().Be((StatusCode)200);
        dto.Metadata.Should().ContainKey("pagination");
        
        var pagination = dto.Metadata!["pagination"];
        pagination.Should().BeEquivalentTo(new
        {
            page = 2,
            pageSize = 5,
            totalCount = 23,
            totalPages = 5,
            hasNextPage = true,
            hasPreviousPage = true
        });
    }

    [Fact]
    public void OkWithPagination_FirstPage_ShouldHaveNoPreviousPage()
    {
        // Arrange
        var data = new List<string> { "a", "b", "c" };

        // Act
        var dto = SuccessResponseDto<List<string>>.OkWithPagination(data, 1, 10, 25);

        // Assert
        var pagination = dto.Metadata!["pagination"];
        pagination.Should().BeEquivalentTo(new
        {
            page = 1,
            pageSize = 10,
            totalCount = 25,
            totalPages = 3,
            hasNextPage = true,
            hasPreviousPage = false
        });
    }

    [Fact]
    public void OkWithPagination_LastPage_ShouldHaveNoNextPage()
    {
        // Arrange
        var data = new List<string> { "x", "y" };

        // Act
        var dto = SuccessResponseDto<List<string>>.OkWithPagination(data, 3, 10, 25);

        // Assert
        var pagination = dto.Metadata!["pagination"];
        pagination.Should().BeEquivalentTo(new
        {
            page = 3,
            pageSize = 10,
            totalCount = 25,
            totalPages = 3,
            hasNextPage = false,
            hasPreviousPage = true
        });
    }

    [Fact]
    public void OkWithPagination_WithDefaultMessage_ShouldUseDefaultMessage()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3 };

        // Act
        var dto = SuccessResponseDto<List<int>>.OkWithPagination(data, 1, 10, 15);

        // Assert
        dto.Message.Should().Be("Data retrieved successfully");
    }

    [Fact]
    public void GenericSuccessResponseDto_WithNullData_ShouldBeAllowed()
    {
        // Act
        var dto = SuccessResponseDto<string?>.Ok(null);

        // Assert
        dto.Data.Should().BeNull();
        dto.Success.Should().BeTrue();
    }

    [Fact]
    public void GenericSuccessResponseDto_ShouldInheritFromSuccessResponseDto()
    {
        // Act
        var dto = SuccessResponseDto<int>.Ok(42);

        // Assert
        dto.Should().BeAssignableTo<SuccessResponseDto>();
        dto.Success.Should().BeTrue();
        dto.StatusCode.Should().Be((StatusCode)200);
    }
}
