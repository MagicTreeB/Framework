using MMO.Core.Exceptions.Base;

namespace MMO.Core.Exceptions.UnitTest.Base;

public class DomainExceptionTests
{
    private class TestDomainException : DomainException
    {
        public TestDomainException(string message, string errorCode) 
            : base(message, errorCode)
        {
        }

        public TestDomainException(string message, string errorCode, Exception innerException)
            : base(message, errorCode, innerException)
        {
        }
    }

    [Fact]
    public void Constructor_Should_SetMessageAndErrorCode()
    {
        // Arrange
        var message = "Test error message";
        var errorCode = "TEST_ERROR";

        // Act
        var exception = new TestDomainException(message, errorCode);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be(errorCode);
        exception.Details.Should().NotBeNull();
        exception.Details.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithInnerException_Should_SetInnerException()
    {
        // Arrange
        var message = "Test error message";
        var errorCode = "TEST_ERROR";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new TestDomainException(message, errorCode, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be(errorCode);
        exception.InnerException.Should().Be(innerException);
        exception.Details.Should().NotBeNull();
        exception.Details.Should().BeEmpty();
    }

    [Fact]
    public void AddDetail_Should_AddKeyValuePairToDetails()
    {
        // Arrange
        var exception = new TestDomainException("Test", "TEST_ERROR");
        var key = "UserId";
        var value = Guid.NewGuid();

        // Act
        exception.AddDetail(key, value);

        // Assert
        exception.Details.Should().ContainKey(key);
        exception.Details[key].Should().Be(value);
    }

    [Fact]
    public void AddDetail_Should_OverwriteExistingKey()
    {
        // Arrange
        var exception = new TestDomainException("Test", "TEST_ERROR");
        var key = "UserId";
        var value1 = Guid.NewGuid();
        var value2 = Guid.NewGuid();

        // Act
        exception.AddDetail(key, value1);
        exception.AddDetail(key, value2);

        // Assert
        exception.Details.Should().ContainKey(key);
        exception.Details[key].Should().Be(value2);
        exception.Details.Should().HaveCount(1);
    }

    [Fact]
    public void AddDetail_Should_AllowMultipleDetails()
    {
        // Arrange
        var exception = new TestDomainException("Test", "TEST_ERROR");

        // Act
        exception.AddDetail("Key1", "Value1");
        exception.AddDetail("Key2", 123);
        exception.AddDetail("Key3", true);

        // Assert
        exception.Details.Should().HaveCount(3);
        exception.Details["Key1"].Should().Be("Value1");
        exception.Details["Key2"].Should().Be(123);
        exception.Details["Key3"].Should().Be(true);
    }
}
