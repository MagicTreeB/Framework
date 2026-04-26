using MMO.Core.Exceptions.Common;

namespace MMO.Core.Exceptions.UnitTest.Common;

public class EntityAlreadyExistsExceptionTests
{
    [Fact]
    public void Constructor_Should_SetMessageAndErrorCode()
    {
        // Arrange
        var entityName = "User";
        var propertyName = "Email";
        var propertyValue = "test@example.com";

        // Act
        var exception = new EntityAlreadyExistsException(entityName, propertyName, propertyValue);

        // Assert
        exception.Message.Should().Be($"{entityName} with {propertyName} '{propertyValue}' already exists.");
        exception.ErrorCode.Should().Be("USER_ALREADY_EXISTS");
        exception.Details.Should().ContainKey("EntityName");
        exception.Details["EntityName"].Should().Be(entityName);
        exception.Details.Should().ContainKey("PropertyName");
        exception.Details["PropertyName"].Should().Be(propertyName);
        exception.Details.Should().ContainKey("PropertyValue");
        exception.Details["PropertyValue"].Should().Be(propertyValue);
    }

    [Fact]
    public void Constructor_WithEmailIdentifier_Should_SetCorrectly()
    {
        // Arrange
        var entityName = "Account";
        var propertyName = "Email";
        var email = "john.doe@example.com";

        // Act
        var exception = new EntityAlreadyExistsException(entityName, propertyName, email);

        // Assert
        exception.Message.Should().Contain(email);
        exception.ErrorCode.Should().Be("ACCOUNT_ALREADY_EXISTS");
        exception.Details["PropertyValue"].Should().Be(email);
    }

    [Fact]
    public void Constructor_WithUsernameIdentifier_Should_SetCorrectly()
    {
        // Arrange
        var entityName = "User";
        var propertyName = "Username";
        var username = "johndoe123";

        // Act
        var exception = new EntityAlreadyExistsException(entityName, propertyName, username);

        // Assert
        exception.Message.Should().Contain(username);
        exception.ErrorCode.Should().Be("USER_ALREADY_EXISTS");
        exception.Details["PropertyValue"].Should().Be(username);
    }

    [Fact]
    public void ErrorCode_Should_BeUpperCaseWithUnderscore()
    {
        // Arrange
        var entityName = "ShortUrl";
        var propertyName = "Code";
        var code = "abc123";

        // Act
        var exception = new EntityAlreadyExistsException(entityName, propertyName, code);

        // Assert
        exception.ErrorCode.Should().Be("SHORTURL_ALREADY_EXISTS");
    }
}
