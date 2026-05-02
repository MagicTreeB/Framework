using Microsoft.AspNetCore.Builder;
using MagicTree.Framework.Exceptions.Extensions;

namespace MagicTree.Framework.Exceptions.UnitTest.Extensions;

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
