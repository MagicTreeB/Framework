using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace MagicTree.Framework.Extensions.UnitTest.MigrationExtensionTests
{
    public class MigrationExtensionTests
    {
        private class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }

            public DbSet<TestEntity> TestEntities => Set<TestEntity>();
        }

        private class TestEntity
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public async Task UseMigrationAsync_WhenFeatureDisabled_ShouldReturnApplication()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<TestDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_Disabled"));
            
            var featureManagerMock = new Mock<IFeatureManager>();
            featureManagerMock.Setup(fm => fm.IsEnabledAsync("AutoMigration"))
                .ReturnsAsync(false);
            services.AddSingleton(featureManagerMock.Object);

            var serviceProvider = services.BuildServiceProvider();
            var app = new ApplicationBuilder(serviceProvider);

            // Act
            var result = await app.UseMigrationAsync<TestDbContext>();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(app);
            featureManagerMock.Verify(fm => fm.IsEnabledAsync("AutoMigration"), Times.Once);
        }

        [Fact]
        public void UseMigrationAsync_Should_ExistAsExtensionMethod()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<TestDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));
            
            var featureManagerMock = new Mock<IFeatureManager>();
            services.AddSingleton(featureManagerMock.Object);

            var serviceProvider = services.BuildServiceProvider();
            var app = new ApplicationBuilder(serviceProvider);

            // Act & Assert - method should exist and be callable
            var method = typeof(MigrationExtension).GetMethod("UseMigrationAsync");
            method.Should().NotBeNull();
        }

        [Fact]
        public void CanConnectToDatabaseAsync_Should_ExistAsExtensionMethod()
        {
            // Arrange & Act & Assert
            var method = typeof(MigrationExtension).GetMethod("CanConnectToDatabaseAsync");
            method.Should().NotBeNull();
        }
    }
}
