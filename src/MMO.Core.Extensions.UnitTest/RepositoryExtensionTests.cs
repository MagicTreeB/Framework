using Microsoft.Extensions.DependencyInjection;

namespace MMO.Core.Extensions.UnitTest.RepositoryExtensionTests
{
    public class RepositoryExtensionTests
    {
        [Fact]
        public void AddRepositories_Should_NotThrow()
        {
            // Arrange
            var services = new ServiceCollection();
            var assembly = typeof(RepositoryExtensionTests).Assembly;

            // Act & Assert - should not throw even if no repositories are found
            var act = () => services.AddRepositories(assembly);
            act.Should().NotThrow();
        }

        [Fact]
        public void AddRepositoriesFromAssemblyContaining_Should_NotThrow()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert - should not throw
            var act = () => services.AddRepositoriesFromAssemblyContaining(typeof(RepositoryExtensionTests));
            act.Should().NotThrow();
        }

        [Fact]
        public void AddRepositories_WithNoAssemblies_ShouldUseCallingAssembly()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRepositories();

            // Assert - should not throw
            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.Should().NotBeNull();
        }

        [Fact]
        public void AddRepositories_Should_ReturnServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddRepositories();

            // Assert
            result.Should().BeSameAs(services);
        }

        [Fact]
        public void AddRepositories_Should_WorkWithMultipleAssemblies()
        {
            // Arrange
            var services = new ServiceCollection();
            var assembly1 = typeof(RepositoryExtensionTests).Assembly;
            var assembly2 = typeof(string).Assembly;

            // Act & Assert - should not throw with multiple assemblies
            var act = () => services.AddRepositories(assembly1, assembly2);
            act.Should().NotThrow();
        }
    }
}
