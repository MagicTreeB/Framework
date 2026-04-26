using MMO.Core.Exceptions.Common;

namespace MMO.Core.Exceptions.UnitTest.Common;

public class UnauthorizedEntityAccessExceptionTests
{
    [Fact]
    public void Constructor_Should_SetMessageAndErrorCode()
    {
        // Arrange
        var entityName = "Document";
        var userId = Guid.NewGuid().ToString();
        var requiredPermission = "Read";

        // Act
        var exception = new UnauthorizedEntityAccessException(entityName, userId, requiredPermission);

        // Assert
        exception.Message.Should().Be($"User '{userId}' does not have permission to access {entityName}. Required: {requiredPermission}");
        exception.ErrorCode.Should().Be("UNAUTHORIZED_DOCUMENT_ACCESS");
        exception.Details.Should().ContainKey("EntityName");
        exception.Details["EntityName"].Should().Be(entityName);
        exception.Details.Should().ContainKey("UserId");
        exception.Details["UserId"].Should().Be(userId);
        exception.Details.Should().ContainKey("RequiredPermission");
        exception.Details["RequiredPermission"].Should().Be(requiredPermission);
    }

    [Fact]
    public void Constructor_WithUserAccess_Should_SetCorrectly()
    {
        // Arrange
        var entityName = "Order";
        var userId = Guid.NewGuid().ToString();
        var requiredPermission = "Modify";

        // Act
        var exception = new UnauthorizedEntityAccessException(entityName, userId, requiredPermission);

        // Assert
        exception.Message.Should().Contain(userId);
        exception.Message.Should().Contain("Order");
        exception.ErrorCode.Should().Be("UNAUTHORIZED_ORDER_ACCESS");
    }

    [Fact]
    public void Constructor_WithFileAccess_Should_SetCorrectly()
    {
        // Arrange
        var entityName = "File";
        var userId = Guid.NewGuid().ToString();
        var requiredPermission = "Delete";

        // Act
        var exception = new UnauthorizedEntityAccessException(entityName, userId, requiredPermission);

        // Assert
        exception.Details["EntityName"].Should().Be("File");
        exception.Details["UserId"].Should().Be(userId);
        exception.ErrorCode.Should().Be("UNAUTHORIZED_FILE_ACCESS");
    }

    [Fact]
    public void Constructor_WithAdminResource_Should_SetCorrectly()
    {
        // Arrange
        var entityName = "AdminPanel";
        var userId = Guid.NewGuid().ToString();
        var requiredPermission = "Admin";

        // Act
        var exception = new UnauthorizedEntityAccessException(entityName, userId, requiredPermission);

        // Assert
        exception.ErrorCode.Should().Be("UNAUTHORIZED_ADMINPANEL_ACCESS");
    }

    [Fact]
    public void ErrorCode_Should_BeUpperCaseWithUnderscore()
    {
        // Arrange
        var entityName = "BlogPost";
        var userId = Guid.NewGuid().ToString();
        var requiredPermission = "Write";

        // Act
        var exception = new UnauthorizedEntityAccessException(entityName, userId, requiredPermission);

        // Assert
        exception.ErrorCode.Should().Be("UNAUTHORIZED_BLOGPOST_ACCESS");
    }
}
