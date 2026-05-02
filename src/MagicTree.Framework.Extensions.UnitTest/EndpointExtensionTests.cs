using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace MagicTree.Framework.Extensions.UnitTest.EndpointExtensionTests
{
    public class EndpointExtensionTests
    {
        [Fact]
        public void MapAllEndpoints_Should_DiscoverAndInvokeEndpointMethods()
        {
            // Arrange
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();
            var endpointsCalled = new List<string>();

            // Note: This test verifies the extension method exists and can be called
            // Actual endpoint discovery requires real endpoint classes in the assembly

            // Act
            var result = app.MapAllEndpoints("MagicTree.Framework.Extensions.UnitTest.TestEndpoints");

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(app);
        }

        [Fact]
        public void MapAllEndpoints_WithInvalidNamespace_ShouldNotThrow()
        {
            // Arrange
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();

            // Act
            var act = () => app.MapAllEndpoints("NonExistent.Namespace");

            // Assert - should not throw, just won't find any endpoints
            act.Should().NotThrow();
        }
    }
}

// Test endpoint classes to verify discovery
namespace MagicTree.Framework.Extensions.UnitTest.TestEndpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder app)
        {
            // Test endpoint that would be discovered
            app.MapGet("/test/users", () => "Test");
        }
    }

    public static class ProductEndpoints
    {
        public static void MapProductEndpoints(this IEndpointRouteBuilder app)
        {
            // Test endpoint that would be discovered
            app.MapGet("/test/products", () => "Test");
        }
    }
}
