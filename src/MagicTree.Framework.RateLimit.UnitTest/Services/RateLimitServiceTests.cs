using MagicTree.Framework.RateLimit.Interfaces;
using MagicTree.Framework.RateLimit.Services;

namespace MagicTree.Framework.RateLimit.UnitTest.Services;

public class RateLimitServiceTests
{
    [Fact]
    public async Task CheckRateLimitAsync_FirstRequest_ShouldAllow()
    {
        // Arrange
        var storageMock = new Mock<IRateLimitStorage>();
        storageMock.Setup(x => x.IncrementAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        storageMock.Setup(x => x.GetTtlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromSeconds(60));

        var service = new RateLimitService(storageMock.Object);

        // Act
        var result = await service.CheckRateLimitAsync("user-123", "/api/test", 10, 60);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.CurrentCount.Should().Be(1);
        result.Limit.Should().Be(10);
        result.Remaining.Should().Be(9);
    }

    [Fact]
    public async Task CheckRateLimitAsync_ExceedsLimit_ShouldDeny()
    {
        // Arrange
        var storageMock = new Mock<IRateLimitStorage>();
        storageMock.Setup(x => x.IncrementAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(11L); // Exceeds limit of 10
        storageMock.Setup(x => x.GetTtlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromSeconds(45));

        var service = new RateLimitService(storageMock.Object);

        // Act
        var result = await service.CheckRateLimitAsync("user-123", "/api/test", 10, 60);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.CurrentCount.Should().Be(11);
        result.Limit.Should().Be(10);
        result.Remaining.Should().Be(0);
    }

    [Fact]
    public async Task CheckRateLimitAsync_AtLimit_ShouldAllow()
    {
        // Arrange
        var storageMock = new Mock<IRateLimitStorage>();
        storageMock.Setup(x => x.IncrementAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10L); // Exactly at limit
        storageMock.Setup(x => x.GetTtlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromSeconds(30));

        var service = new RateLimitService(storageMock.Object);

        // Act
        var result = await service.CheckRateLimitAsync("user-123", "/api/test", 10, 60);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.CurrentCount.Should().Be(10);
        result.Remaining.Should().Be(0);
    }

    [Fact]
    public async Task CheckRateLimitAsync_ShouldBuildCorrectKey()
    {
        // Arrange
        var storageMock = new Mock<IRateLimitStorage>();
        string? capturedKey = null;
        
        storageMock.Setup(x => x.IncrementAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L)
            .Callback<string, TimeSpan, CancellationToken>((key, _, _) => capturedKey = key);

        var service = new RateLimitService(storageMock.Object);

        // Act
        await service.CheckRateLimitAsync("user-123", "/api/test/endpoint", 10, 60);

        // Assert
        capturedKey.Should().Be("ratelimit:user-123:api:test:endpoint");
    }

    [Fact]
    public async Task CheckRateLimitAsync_WithQueryString_ShouldRemoveQueryString()
    {
        // Arrange
        var storageMock = new Mock<IRateLimitStorage>();
        string? capturedKey = null;
        
        storageMock.Setup(x => x.IncrementAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L)
            .Callback<string, TimeSpan, CancellationToken>((key, _, _) => capturedKey = key);

        var service = new RateLimitService(storageMock.Object);

        // Act
        await service.CheckRateLimitAsync("user-123", "/api/test?param=value", 10, 60);

        // Assert
        capturedKey.Should().Be("ratelimit:user-123:api:test");
    }

    [Fact]
    public async Task ResetAsync_ShouldCallStorageReset()
    {
        // Arrange
        var storageMock = new Mock<IRateLimitStorage>();
        string? capturedKey = null;
        
        storageMock.Setup(x => x.ResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<string, CancellationToken>((key, _) => capturedKey = key);

        var service = new RateLimitService(storageMock.Object);

        // Act
        await service.ResetAsync("user-123", "/api/test");

        // Assert
        storageMock.Verify(x => x.ResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        capturedKey.Should().Be("ratelimit:user-123:api:test");
    }

    [Fact]
    public async Task CheckRateLimitAsync_WithoutTTL_ShouldUseWindowSeconds()
    {
        // Arrange
        var storageMock = new Mock<IRateLimitStorage>();
        storageMock.Setup(x => x.IncrementAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        storageMock.Setup(x => x.GetTtlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TimeSpan?)null); // No TTL

        var service = new RateLimitService(storageMock.Object);

        // Act
        var result = await service.CheckRateLimitAsync("user-123", "/api/test", 10, 60);

        // Assert
        result.RetryAfterSeconds.Should().Be(60);
        result.ResetAt.Should().BeGreaterThan(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }
}
