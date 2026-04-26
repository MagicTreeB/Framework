using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MMO.Core.Extensions.UnitTest.CqrsExtensionTests
{
    public class CqrsExtensionTests
    {
        [Fact]
        public void AddCqrsHandlers_Should_NotThrow()
        {
            // Arrange
            var services = new ServiceCollection();
            var assembly = typeof(CqrsExtensionTests).Assembly;

            // Act & Assert - should not throw even if no handlers are found
            var act = () => services.AddCqrsHandlers(assembly);
            act.Should().NotThrow();
        }

        [Fact]
        public void AddCqrsHandlersFromAssemblyContaining_Should_NotThrow()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert - should not throw
            var act = () => services.AddCqrsHandlersFromAssemblyContaining(typeof(CqrsExtensionTests));
            act.Should().NotThrow();
        }

        [Fact]
        public void AddCqrsHandlers_WithNoAssemblies_ShouldUseCallingAssembly()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddCqrsHandlers();

            // Assert - should not throw
            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.Should().NotBeNull();
        }

        [Fact]
        public void AddCqrsHandlers_Should_ReturnServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddCqrsHandlers();

            // Assert
            result.Should().BeSameAs(services);
        }
    }
}
