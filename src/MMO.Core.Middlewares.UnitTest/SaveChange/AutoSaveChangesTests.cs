using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MMO.Core.Middlewares.SaveChange;

namespace MMO.Core.Middlewares.UnitTest.SaveChange
{
    public class AutoSaveChangesTests
    {
        private class TestEntity
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }

            public DbSet<TestEntity> TestEntities => Set<TestEntity>();
        }

        [Fact]
        public async Task InvokeAsync_WithPostRequest_ShouldSaveChanges()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_Post")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            var tracker = new DbContextTracker();
            services.AddSingleton<IDbContextTracker>(tracker);

            var serviceProvider = services.BuildServiceProvider();
            var context = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
                Request = { Method = "POST" },
                Response = { StatusCode = 200 }
            };

            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
            dbContext.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Test" });
            tracker.Track(dbContext);

            var nextCalled = false;
            RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };

            var middleware = new AutoSaveChangesMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeTrue();
            dbContext.ChangeTracker.HasChanges().Should().BeFalse();
        }

        [Fact]
        public async Task InvokeAsync_WithPutRequest_ShouldSaveChanges()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_Put")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            var tracker = new DbContextTracker();
            services.AddSingleton<IDbContextTracker>(tracker);

            var serviceProvider = services.BuildServiceProvider();
            var context = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
                Request = { Method = "PUT" },
                Response = { StatusCode = 200 }
            };

            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Original" };
            dbContext.TestEntities.Add(entity);
            await dbContext.SaveChangesAsync();
            
            entity.Name = "Updated";
            tracker.Track(dbContext);

            var nextCalled = false;
            RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };

            var middleware = new AutoSaveChangesMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeTrue();
            dbContext.ChangeTracker.HasChanges().Should().BeFalse();
        }

        [Fact]
        public async Task InvokeAsync_WithDeleteRequest_ShouldSaveChanges()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_Delete")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            var tracker = new DbContextTracker();
            services.AddSingleton<IDbContextTracker>(tracker);

            var serviceProvider = services.BuildServiceProvider();
            var context = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
                Request = { Method = "DELETE" },
                Response = { StatusCode = 200 }
            };

            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "ToDelete" };
            dbContext.TestEntities.Add(entity);
            await dbContext.SaveChangesAsync();
            
            dbContext.TestEntities.Remove(entity);
            tracker.Track(dbContext);

            var nextCalled = false;
            RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };

            var middleware = new AutoSaveChangesMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeTrue();
            dbContext.ChangeTracker.HasChanges().Should().BeFalse();
        }

        [Fact]
        public async Task InvokeAsync_WithGetRequest_ShouldNotSaveChanges()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_Get")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            var tracker = new DbContextTracker();
            services.AddSingleton<IDbContextTracker>(tracker);

            var serviceProvider = services.BuildServiceProvider();
            var context = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
                Request = { Method = "GET" },
                Response = { StatusCode = 200 }
            };

            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
            dbContext.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Test" });
            tracker.Track(dbContext);

            var nextCalled = false;
            RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };

            var middleware = new AutoSaveChangesMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeTrue();
            dbContext.ChangeTracker.HasChanges().Should().BeTrue(); // Changes should still exist
        }

        [Fact]
        public async Task InvokeAsync_WithErrorResponse_ShouldNotSaveChanges()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_Error")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            var tracker = new DbContextTracker();
            services.AddSingleton<IDbContextTracker>(tracker);

            var serviceProvider = services.BuildServiceProvider();
            var context = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
                Request = { Method = "POST" },
                Response = { StatusCode = 400 } // Bad Request
            };

            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
            dbContext.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Test" });
            tracker.Track(dbContext);

            var nextCalled = false;
            RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };

            var middleware = new AutoSaveChangesMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeTrue();
            dbContext.ChangeTracker.HasChanges().Should().BeTrue(); // Changes should still exist
        }

        [Fact]
        public async Task InvokeAsync_WithNoChanges_ShouldNotCallSaveChanges()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_NoChanges")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            var tracker = new DbContextTracker();
            services.AddSingleton<IDbContextTracker>(tracker);

            var serviceProvider = services.BuildServiceProvider();
            var context = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
                Request = { Method = "POST" },
                Response = { StatusCode = 200 }
            };

            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
            tracker.Track(dbContext);

            var nextCalled = false;
            RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };

            var middleware = new AutoSaveChangesMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeTrue();
            dbContext.ChangeTracker.HasChanges().Should().BeFalse();
        }

        [Fact]
        public async Task DbContextTracker_Should_TrackMultipleContexts()
        {
            // Arrange
            var tracker = new DbContextTracker();
            var options1 = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase("TestDb1")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            var options2 = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase("TestDb2")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var context1 = new TestDbContext(options1);
            var context2 = new TestDbContext(options2);

            // Act
            context1.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Context1" });
            context2.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Context2" });
            
            tracker.Track(context1);
            tracker.Track(context2);
            await tracker.SaveAllChangesAsync();

            // Assert - both contexts should have saved changes
            context1.ChangeTracker.HasChanges().Should().BeFalse();
            context2.ChangeTracker.HasChanges().Should().BeFalse();
        }

        [Fact]
        public async Task InvokeAsync_WithPatchRequest_ShouldSaveChanges()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_Patch")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            var tracker = new DbContextTracker();
            services.AddSingleton<IDbContextTracker>(tracker);

            var serviceProvider = services.BuildServiceProvider();
            var context = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
                Request = { Method = "PATCH" },
                Response = { StatusCode = 200 }
            };

            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Original" };
            dbContext.TestEntities.Add(entity);
            await dbContext.SaveChangesAsync();
            
            entity.Name = "Patched";
            tracker.Track(dbContext);

            var nextCalled = false;
            RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };

            var middleware = new AutoSaveChangesMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeTrue();
            dbContext.ChangeTracker.HasChanges().Should().BeFalse();
        }
    }
}
