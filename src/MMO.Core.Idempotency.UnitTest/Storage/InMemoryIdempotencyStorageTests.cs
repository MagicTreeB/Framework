using Microsoft.Extensions.Caching.Memory;
using MMO.Core.Idempotency.Models;
using MMO.Core.Idempotency.Storage;

namespace MMO.Core.Idempotency.UnitTest.Storage;

public class InMemoryIdempotencyStorageTests
{
    [Fact]
    public async Task GetAsync_NonExistentKey_ShouldReturnNull()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryIdempotencyStorage(cache);

        // Act
        var result = await storage.GetAsync("nonexistent-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldStoreRecord()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryIdempotencyStorage(cache);
        var record = new IdempotencyRecord
        {
            Key = "test-key",
            StatusCode = 200,
            ResponseBody = "Success",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };

        // Act
        await storage.SetAsync(record);
        var retrieved = await storage.GetAsync("test-key");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Key.Should().Be("test-key");
        retrieved.StatusCode.Should().Be(200);
        retrieved.ResponseBody.Should().Be("Success");
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_NewKey_ShouldReturnTrue()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryIdempotencyStorage(cache);

        // Act
        var result = await storage.TryMarkAsProcessingAsync("new-key", "POST", "/api/test", 24);

        // Assert
        result.Should().BeTrue();
        var record = await storage.GetAsync("new-key");
        record.Should().NotBeNull();
        record!.IsProcessing.Should().BeTrue();
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_AlreadyProcessing_ShouldReturnFalse()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryIdempotencyStorage(cache);
        await storage.TryMarkAsProcessingAsync("existing-key", "POST", "/api/test", 24);

        // Act
        var result = await storage.TryMarkAsProcessingAsync("existing-key", "POST", "/api/test", 24);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryMarkAsProcessingAsync_AlreadyCompleted_ShouldReturnFalse()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryIdempotencyStorage(cache);
        var completedRecord = new IdempotencyRecord
        {
            Key = "completed-key",
            StatusCode = 200,
            IsProcessing = false,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        await storage.SetAsync(completedRecord);

        // Act
        var result = await storage.TryMarkAsProcessingAsync("completed-key", "POST", "/api/test", 24);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveProcessingMarkAsync_ShouldRemoveRecord()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryIdempotencyStorage(cache);
        await storage.TryMarkAsProcessingAsync("processing-key", "POST", "/api/test", 24);

        // Act
        await storage.RemoveProcessingMarkAsync("processing-key");
        var record = await storage.GetAsync("processing-key");

        // Assert
        record.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ExistingKey_ShouldReturnTrue()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryIdempotencyStorage(cache);
        var record = new IdempotencyRecord
        {
            Key = "existing-key",
            StatusCode = 200,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        await storage.SetAsync(record);

        // Act
        var exists = await storage.ExistsAsync("existing-key");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistentKey_ShouldReturnFalse()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryIdempotencyStorage(cache);

        // Act
        var exists = await storage.ExistsAsync("nonexistent-key");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveRecord()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryIdempotencyStorage(cache);
        var record = new IdempotencyRecord
        {
            Key = "delete-key",
            StatusCode = 200,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        await storage.SetAsync(record);

        // Act
        await storage.DeleteAsync("delete-key");
        var exists = await storage.ExistsAsync("delete-key");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task SetAsync_WithExpiration_ShouldRespectExpiresAt()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var storage = new InMemoryIdempotencyStorage(cache);
        var record = new IdempotencyRecord
        {
            Key = "expiring-key",
            StatusCode = 200,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
        };

        // Act
        await storage.SetAsync(record);
        var retrieved = await storage.GetAsync("expiring-key");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.ExpiresAt.Should().BeCloseTo(record.ExpiresAt, TimeSpan.FromSeconds(1));
    }
}
