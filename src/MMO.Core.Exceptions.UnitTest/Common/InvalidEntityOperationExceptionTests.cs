using MMO.Core.Exceptions.Common;

namespace MMO.Core.Exceptions.UnitTest.Common;

public class InvalidEntityOperationExceptionTests
{
    [Fact]
    public void Constructor_Should_SetMessageAndErrorCode()
    {
        // Arrange
        var entityName = "User";
        var operation = "Activate";
        var currentState = "AlreadyActive";

        // Act
        var exception = new InvalidEntityOperationException(entityName, operation, currentState);

        // Assert
        exception.Message.Should().Be($"Cannot perform '{operation}' on {entityName} in '{currentState}' state.");
        exception.ErrorCode.Should().Be("INVALID_USER_OPERATION");
        exception.Details.Should().ContainKey("EntityName");
        exception.Details["EntityName"].Should().Be(entityName);
        exception.Details.Should().ContainKey("Operation");
        exception.Details["Operation"].Should().Be(operation);
        exception.Details.Should().ContainKey("CurrentState");
        exception.Details["CurrentState"].Should().Be(currentState);
    }

    [Fact]
    public void Constructor_WithActivateOperation_Should_SetCorrectly()
    {
        // Arrange
        var entityName = "Account";
        var operation = "Activate";
        var currentState = "Suspended";

        // Act
        var exception = new InvalidEntityOperationException(entityName, operation, currentState);

        // Assert
        exception.Message.Should().Contain("Activate");
        exception.Message.Should().Contain("Suspended");
        exception.ErrorCode.Should().Be("INVALID_ACCOUNT_OPERATION");
    }

    [Fact]
    public void Constructor_WithDeleteOperation_Should_SetCorrectly()
    {
        // Arrange
        var entityName = "Order";
        var operation = "Delete";
        var currentState = "Completed";

        // Act
        var exception = new InvalidEntityOperationException(entityName, operation, currentState);

        // Assert
        exception.Details["Operation"].Should().Be("Delete");
        exception.Details["CurrentState"].Should().Be("Completed");
        exception.ErrorCode.Should().Be("INVALID_ORDER_OPERATION");
    }

    [Fact]
    public void Constructor_WithPublishOperation_Should_SetCorrectly()
    {
        // Arrange
        var entityName = "BlogPost";
        var operation = "Publish";
        var currentState = "Draft";

        // Act
        var exception = new InvalidEntityOperationException(entityName, operation, currentState);

        // Assert
        exception.Message.Should().Contain("Publish");
        exception.ErrorCode.Should().Be("INVALID_BLOGPOST_OPERATION");
    }

    [Fact]
    public void ErrorCode_Should_BeUpperCaseWithUnderscore()
    {
        // Arrange
        var entityName = "ShortUrl";
        var operation = "Extend";
        var currentState = "Expired";

        // Act
        var exception = new InvalidEntityOperationException(entityName, operation, currentState);

        // Assert
        exception.ErrorCode.Should().Be("INVALID_SHORTURL_OPERATION");
    }
}
