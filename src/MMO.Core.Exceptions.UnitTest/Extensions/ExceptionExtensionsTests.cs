using Microsoft.AspNetCore.Builder;
using MMO.Core.Exceptions.Extensions;

namespace MMO.Core.Exceptions.UnitTest.Extensions;

public class ExceptionExtensionsTests
{
    [Fact]
    public void UseGlobalExceptionHandler_ShouldReturnApplicationBuilder()
    {
        // Arrange
        var appBuilder = WebApplication.CreateBuilder().Build();

        // Act
        var result = appBuilder.UseGlobalExceptionHandler();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(appBuilder);
    }
}
