using Microsoft.AspNetCore.Http;
using MagicTree.Framework.Middlewares.Jwt;
using System.Security.Claims;

namespace MagicTree.Framework.Middlewares.UnitTest.Jwt
{
    public class JwtMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_WithAuthenticatedUser_ShouldExtractJwtInfo()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("sub", "user-123"),
                new Claim("name", "John Doe"),
                new Claim("email", "john@example.com"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var context = new DefaultHttpContext { User = principal };
            var nextCalled = false;
            RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };

            var middleware = new JwtMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeTrue();
            var jwtInfo = context.GetJwtInfo();
            jwtInfo.Should().NotBeNull();
            jwtInfo!.UserId.Should().Be("user-123");
            jwtInfo.Username.Should().Be("John Doe");
            jwtInfo.Email.Should().Be("john@example.com");
            jwtInfo.Roles.Should().Contain("Admin");
            jwtInfo.Roles.Should().HaveCount(1);
        }

        [Fact]
        public async Task InvokeAsync_WithUnauthenticatedUser_ShouldNotSetJwtInfo()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var nextCalled = false;
            RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };

            var middleware = new JwtMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeTrue();
            var jwtInfo = context.GetJwtInfo();
            jwtInfo.Should().BeNull();
        }

        [Fact]
        public async Task InvokeAsync_WithoutUserIdClaim_ShouldNotSetJwtInfo()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("name", "John Doe"),
                new Claim("email", "john@example.com")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var context = new DefaultHttpContext { User = principal };
            var nextCalled = false;
            RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };

            var middleware = new JwtMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeTrue();
            var jwtInfo = context.GetJwtInfo();
            jwtInfo.Should().BeNull();
        }

        [Fact]
        public void GetJwtInfo_WhenNotSet_ShouldReturnNull()
        {
            // Arrange
            var context = new DefaultHttpContext();

            // Act
            var jwtInfo = context.GetJwtInfo();

            // Assert
            jwtInfo.Should().BeNull();
        }

        [Fact]
        public void GetUserId_WithValidJwtInfo_ShouldReturnUserId()
        {
            // Arrange
            var claims = new List<Claim> { new Claim("sub", "user-456") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var context = new DefaultHttpContext { User = principal };
            
            var jwtInfo = new JwtInfo
            {
                UserId = "user-456",
                Username = "TestUser",
                Email = "test@example.com",
                Roles = new List<string> { "User" },
                Claims = new Dictionary<string, string>()
            };
            context.Items["JwtInfo"] = jwtInfo;

            // Act
            var userId = context.GetUserId();

            // Assert
            userId.Should().Be("user-456");
        }

        [Fact]
        public void GetUserId_WithoutJwtInfo_ShouldReturnNull()
        {
            // Arrange
            var context = new DefaultHttpContext();

            // Act
            var userId = context.GetUserId();

            // Assert
            userId.Should().BeNull();
        }

        [Fact]
        public void GetUsername_WithValidJwtInfo_ShouldReturnUsername()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var jwtInfo = new JwtInfo
            {
                UserId = "user-123",
                Username = "JohnDoe",
                Email = "john@example.com",
                Roles = new List<string>(),
                Claims = new Dictionary<string, string>()
            };
            context.Items["JwtInfo"] = jwtInfo;

            // Act
            var username = context.GetUsername();

            // Assert
            username.Should().Be("JohnDoe");
        }

        [Fact]
        public void GetUserEmail_WithValidJwtInfo_ShouldReturnEmail()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var jwtInfo = new JwtInfo
            {
                UserId = "user-123",
                Username = "JohnDoe",
                Email = "john@example.com",
                Roles = new List<string>(),
                Claims = new Dictionary<string, string>()
            };
            context.Items["JwtInfo"] = jwtInfo;

            // Act
            var email = context.GetUserEmail();

            // Assert
            email.Should().Be("john@example.com");
        }

        [Fact]
        public void GetUserRoles_WithValidJwtInfo_ShouldReturnRoles()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var jwtInfo = new JwtInfo
            {
                UserId = "user-123",
                Username = "JohnDoe",
                Email = "john@example.com",
                Roles = new List<string> { "Admin", "Manager", "User" },
                Claims = new Dictionary<string, string>()
            };
            context.Items["JwtInfo"] = jwtInfo;

            // Act
            var roles = context.GetUserRoles();

            // Assert
            roles.Should().HaveCount(3);
            roles.Should().Contain("Admin");
            roles.Should().Contain("Manager");
            roles.Should().Contain("User");
        }

        [Fact]
        public void GetUserRoles_WithoutJwtInfo_ShouldReturnEmptyList()
        {
            // Arrange
            var context = new DefaultHttpContext();

            // Act
            var roles = context.GetUserRoles();

            // Assert
            roles.Should().NotBeNull();
            roles.Should().BeEmpty();
        }

        [Fact]
        public async Task InvokeAsync_ShouldCacheJwtInfoInHttpContextItems()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("sub", "user-789"),
                new Claim("name", "Jane Smith")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var context = new DefaultHttpContext { User = principal };
            RequestDelegate next = (ctx) => Task.CompletedTask;

            var middleware = new JwtMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Items.Should().ContainKey("JwtInfo");
            var cachedInfo = context.Items["JwtInfo"] as JwtInfo;
            cachedInfo.Should().NotBeNull();
            cachedInfo!.UserId.Should().Be("user-789");
        }

        [Fact]
        public async Task JwtInfo_Should_StoreClaims()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("sub", "user-123"),
                new Claim("custom_claim", "custom_value"),
                new Claim("department", "Engineering")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var context = new DefaultHttpContext { User = principal };
            RequestDelegate next = (ctx) => Task.CompletedTask;
            var middleware = new JwtMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            var jwtInfo = context.GetJwtInfo();
            jwtInfo.Should().NotBeNull();
            jwtInfo!.Claims.Should().ContainKey("custom_claim");
            jwtInfo.Claims["custom_claim"].Should().Be("custom_value");
            jwtInfo.Claims.Should().ContainKey("department");
            jwtInfo.Claims["department"].Should().Be("Engineering");
        }
    }
}
