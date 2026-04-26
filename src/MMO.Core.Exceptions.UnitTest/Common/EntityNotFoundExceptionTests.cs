using MMO.Core.Exceptions.Common;

namespace MMO.Core.Exceptions.UnitTest.Common;

public class EntityNotFoundExceptionTests
{
    [Fact]
    public void Constructor_WithGuidKey_Should_SetMessageAndErrorCode()
    {
        // Arrange
        var entityName = "User";
        var id = Guid.NewGuid();

        // Act
        var exception = new EntityNotFoundException<Guid>(entityName, id);

        // Assert
        exception.Message.Should().Be($"{entityName} with ID '{id}' was not found.");
        exception.ErrorCode.Should().Be("USER_NOT_FOUND");
        exception.Details.Should().ContainKey("EntityName");
        exception.Details["EntityName"].Should().Be(entityName);
        exception.Details.Should().ContainKey("EntityId");
        exception.Details["EntityId"].Should().Be(id);
    }

    [Fact]
    public void Constructor_WithIntKey_Should_SetMessageAndErrorCode()
    {
        // Arrange
        var entityName = "Product";
        var id = 12345;

        // Act
        var exception = new EntityNotFoundException<int>(entityName, id);

        // Assert
        exception.Message.Should().Be($"{entityName} with ID '{id}' was not found.");
        exception.ErrorCode.Should().Be("PRODUCT_NOT_FOUND");
        exception.Details.Should().ContainKey("EntityName");
        exception.Details["EntityName"].Should().Be(entityName);
        exception.Details.Should().ContainKey("EntityId");
        exception.Details["EntityId"].Should().Be(id);
    }

    [Fact]
    public void Constructor_WithStringKey_Should_SetMessageAndErrorCode()
    {
        // Arrange
        var entityName = "Document";
        var id = "DOC-2025-001";

        // Act
        var exception = new EntityNotFoundException<string>(entityName, id);

        // Assert
        exception.Message.Should().Be($"{entityName} with ID '{id}' was not found.");
        exception.ErrorCode.Should().Be("DOCUMENT_NOT_FOUND");
        exception.Details.Should().ContainKey("EntityName");
        exception.Details["EntityName"].Should().Be(entityName);
        exception.Details.Should().ContainKey("EntityId");
        exception.Details["EntityId"].Should().Be(id);
    }

    [Fact]
    public void ErrorCode_Should_BeUpperCaseWithUnderscore()
    {
        // Arrange
        var entityName = "ShortUrl";
        var id = Guid.NewGuid();

        // Act
        var exception = new EntityNotFoundException<Guid>(entityName, id);

        // Assert
        exception.ErrorCode.Should().Be("SHORTURL_NOT_FOUND");
    }

    [Fact]
    public void ErrorCode_Should_HandleMultipleWords()
    {
        // Arrange
        var entityName = "BlogPost";
        var id = Guid.NewGuid();

        // Act
        var exception = new EntityNotFoundException<Guid>(entityName, id);

        // Assert
        exception.ErrorCode.Should().Be("BLOGPOST_NOT_FOUND");
    }
}
