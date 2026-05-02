using Microsoft.Extensions.Caching.Memory;
using MagicTree.Framework.RateLimit.Storage;

namespace MagicTree.Framework.RateLimit.UnitTest.Storage;

public class InMemoryRateLimitStorageTests
{
    [Fact]
    public async Task IncrementAsync_FirstCall_ShouldReturnOne()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryRateLimitStorage(cache);

        // Act
        var count = await storage.IncrementAsync("test-key", TimeSpan.FromMinutes(1));

        // Assert
        count.Should().Be(1);
    }

    [Fact]
    public async Task IncrementAsync_MultipleCalls_ShouldIncrement()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryRateLimitStorage(cache);

        // Act
        var count1 = await storage.IncrementAsync("test-key", TimeSpan.FromMinutes(1));
        var count2 = await storage.IncrementAsync("test-key", TimeSpan.FromMinutes(1));
        var count3 = await storage.IncrementAsync("test-key", TimeSpan.FromMinutes(1));

        // Assert
        count1.Should().Be(1);
        count2.Should().Be(2);
        count3.Should().Be(3);
    }

    [Fact]
    public async Task GetCountAsync_ExistingKey_ShouldReturnCount()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryRateLimitStorage(cache);
        await storage.IncrementAsync("test-key", TimeSpan.FromMinutes(1));
        await storage.IncrementAsync("test-key", TimeSpan.FromMinutes(1));

        // Act
        var count = await storage.GetCountAsync("test-key");

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetCountAsync_NonExistentKey_ShouldReturnZero()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryRateLimitStorage(cache);

        // Act
        var count = await storage.GetCountAsync("nonexistent-key");

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetTtlAsync_ExistingKey_ShouldReturnTTL()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryRateLimitStorage(cache);
        await storage.IncrementAsync("test-key", TimeSpan.FromSeconds(60));

        // Act
        var ttl = await storage.GetTtlAsync("test-key");

        // Assert
        ttl.Should().NotBeNull();
        ttl!.Value.TotalSeconds.Should().BeLessThanOrEqualTo(60);
        ttl.Value.TotalSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetTtlAsync_NonExistentKey_ShouldReturnNull()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryRateLimitStorage(cache);

        // Act
        var ttl = await storage.GetTtlAsync("nonexistent-key");

        // Assert
        ttl.Should().BeNull();
    }

    [Fact]
    public async Task ResetAsync_ShouldClearCounter()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryRateLimitStorage(cache);
        await storage.IncrementAsync("test-key", TimeSpan.FromMinutes(1));
        await storage.IncrementAsync("test-key", TimeSpan.FromMinutes(1));

        // Act
        await storage.ResetAsync("test-key");
        var count = await storage.GetCountAsync("test-key");

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task ExistsAsync_ExistingKey_ShouldReturnTrue()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryRateLimitStorage(cache);
        await storage.IncrementAsync("test-key", TimeSpan.FromMinutes(1));

        // Act
        var exists = await storage.ExistsAsync("test-key");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistentKey_ShouldReturnFalse()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryRateLimitStorage(cache);

        // Act
        var exists = await storage.ExistsAsync("nonexistent-key");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task IncrementAsync_WithExpiration_ShouldMaintainSameExpiration()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryRateLimitStorage(cache);

        // Act
        await storage.IncrementAsync("test-key", TimeSpan.FromMinutes(1));
        var ttl1 = await storage.GetTtlAsync("test-key");
        
        await Task.Delay(100); // Wait a bit
        
        await storage.IncrementAsync("test-key", TimeSpan.FromMinutes(1));
        var ttl2 = await storage.GetTtlAsync("test-key");

        // Assert
        ttl1.Should().NotBeNull();
        ttl2.Should().NotBeNull();
        // Second TTL should be less than first (time elapsed)
        ttl2!.Value.Should().BeLessThan(ttl1!.Value);
    }
}
