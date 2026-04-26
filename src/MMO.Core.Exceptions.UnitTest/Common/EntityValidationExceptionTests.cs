using MMO.Core.Exceptions.Common;

namespace MMO.Core.Exceptions.UnitTest.Common;

public class EntityValidationExceptionTests
{
    [Fact]
    public void Constructor_Should_SetMessageAndErrorCode()
    {
        // Arrange
        var entityName = "User";
        var validationMessage = "Email address is required.";

        // Act
        var exception = new EntityValidationException(entityName, validationMessage);

        // Assert
        exception.Message.Should().Be(validationMessage);
        exception.ErrorCode.Should().Be("USER_VALIDATION_ERROR");
        exception.Details.Should().ContainKey("EntityName");
        exception.Details["EntityName"].Should().Be(entityName);
    }

    [Fact]
    public void Constructor_WithEmailValidation_Should_SetCorrectly()
    {
        // Arrange
        var entityName = "Account";
        var validationMessage = "Email format is invalid.";

        // Act
        var exception = new EntityValidationException(entityName, validationMessage);

        // Assert
        exception.Message.Should().Be(validationMessage);
        exception.ErrorCode.Should().Be("ACCOUNT_VALIDATION_ERROR");
    }

    [Fact]
    public void Constructor_WithPasswordValidation_Should_SetCorrectly()
    {
        // Arrange
        var entityName = "User";
        var validationMessage = "Password must be at least 8 characters.";

        // Act
        var exception = new EntityValidationException(entityName, validationMessage);

        // Assert
        exception.Message.Should().Be(validationMessage);
        exception.Details["EntityName"].Should().Be(entityName);
    }

    [Fact]
    public void Constructor_WithRequiredFieldValidation_Should_SetCorrectly()
    {
        // Arrange
        var entityName = "Product";
        var validationMessage = "Name is required.";

        // Act
        var exception = new EntityValidationException(entityName, validationMessage);

        // Assert
        exception.ErrorCode.Should().Be("PRODUCT_VALIDATION_ERROR");
        exception.Message.Should().Be(validationMessage);
    }

    [Fact]
    public void ErrorCode_Should_BeUpperCaseWithUnderscore()
    {
        // Arrange
        var entityName = "BlogPost";
        var validationMessage = "Title cannot exceed 200 characters.";

        // Act
        var exception = new EntityValidationException(entityName, validationMessage);

        // Assert
        exception.ErrorCode.Should().Be("BLOGPOST_VALIDATION_ERROR");
    }
}
