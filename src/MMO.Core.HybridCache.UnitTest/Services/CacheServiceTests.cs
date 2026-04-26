using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using MMO.Core.HybridCache.Options;
using MMO.Core.HybridCache.Services;

namespace MMO.Core.HybridCache.UnitTest.Services;

/// <summary>
/// Integration tests for CacheService using real HybridCache instance.
/// Note: HybridCache is a sealed class with non-virtual methods, so we can't mock it.
/// </summary>
public class CacheServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Microsoft.Extensions.Caching.Hybrid.HybridCache _hybridCache;
    private readonly CacheService _cacheService;

    public CacheServiceTests()
    {
        var services = new ServiceCollection();
        
        // Add memory cache for L1
        services.AddMemoryCache();
        
        // Add distributed cache for L2 (in-memory implementation for testing)
        services.AddDistributedMemoryCache();
        
        // Add HybridCache with test configuration
        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024; // 1 MB
            options.MaximumKeyLength = 512;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            };
        });
        
        _serviceProvider = services.BuildServiceProvider();
        _hybridCache = _serviceProvider.GetRequiredService<Microsoft.Extensions.Caching.Hybrid.HybridCache>();
        
        var options = new HybridCacheConfig
        {
            DefaultExpirationMinutes = 5,
            LocalCacheExpirationMinutes = 1
        };
        
        _cacheService = new CacheService(_hybridCache, options);
    }

    [Fact]
    public async Task GetOrCreateAsync_CacheMiss_ShouldExecuteFactory()
    {
        // Arrange
        var factoryCallCount = 0;
        var expectedValue = "cached-value";

        // Act
        var result = await _cacheService.GetOrCreateAsync(
            "test-key-1",
            async ct =>
            {
                factoryCallCount++;
                await Task.Delay(10, ct);
                return expectedValue;
            });

        // Assert
        result.Should().Be(expectedValue);
        factoryCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_CacheHit_ShouldNotExecuteFactory()
    {
        // Arrange
        var factoryCallCount = 0;
        var expectedValue = "cached-value";

        // First call - cache miss
        await _cacheService.GetOrCreateAsync(
            "test-key-2",
            async ct =>
            {
                factoryCallCount++;
                await Task.Delay(10, ct);
                return expectedValue;
            });

        // Act - Second call - cache hit
        var result = await _cacheService.GetOrCreateAsync(
            "test-key-2",
            async ct =>
            {
                factoryCallCount++;
                await Task.Delay(10, ct);
                return "should-not-be-called";
            });

        // Assert
        result.Should().Be(expectedValue);
        factoryCallCount.Should().Be(1, "factory should only be called once for cache miss");
    }

    [Fact]
    public async Task GetOrCreateAsync_WithCustomExpiration_ShouldReturnValue()
    {
        // Arrange
        var expectedValue = 42;

        // Act
        var result = await _cacheService.GetOrCreateAsync(
            "test-key-3",
            async ct => await Task.FromResult(expectedValue),
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(2));

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task GetOrCreateAsync_WithTags_ShouldCacheValue()
    {
        // Arrange
        var expectedValue = new { Id = 1, Name = "Test" };
        var tags = new[] { "user:123", "profile" };

        // Act
        var result = await _cacheService.GetOrCreateAsync(
            "test-key-4",
            async ct => await Task.FromResult(expectedValue),
            tags);

        // Assert
        result.Should().BeEquivalentTo(expectedValue);
    }

    [Fact]
    public async Task SetAsync_ThenGetOrCreateAsync_ShouldReturnSetValue()
    {
        // Arrange
        var setValue = "set-value";

        // Act - Set value directly
        await _cacheService.SetAsync("test-key-5", setValue);

        // Act - Try to get or create (should return set value, not execute factory)
        var factoryCalled = false;
        var result = await _cacheService.GetOrCreateAsync(
            "test-key-5",
            async ct =>
            {
                factoryCalled = true;
                await Task.CompletedTask;
                return "factory-value";
            });

        // Assert
        result.Should().Be(setValue);
        factoryCalled.Should().BeFalse("factory should not be called when value already exists");
    }

    [Fact]
    public async Task RemoveAsync_ShouldInvalidateCache()
    {
        // Arrange
        var factoryCallCount = 0;
        var key = "test-key-6";

        // First call - cache miss
        await _cacheService.GetOrCreateAsync(
            key,
            async ct =>
            {
                factoryCallCount++;
                await Task.CompletedTask;
                return "value";
            });

        // Act - Remove from cache
        await _cacheService.RemoveAsync(key);

        // Act - Second call - should be cache miss again
        await _cacheService.GetOrCreateAsync(
            key,
            async ct =>
            {
                factoryCallCount++;
                await Task.CompletedTask;
                return "value";
            });

        // Assert
        factoryCallCount.Should().Be(2, "factory should be called twice: initial miss and after removal");
    }

    [Fact]
    public async Task RemoveByTagAsync_ShouldInvalidateTaggedEntries()
    {
        // Arrange
        var tag = "user:999";
        var factoryCallCount = 0;

        // Create two entries with the same tag
        await _cacheService.GetOrCreateAsync(
            "test-key-7a",
            async ct =>
            {
                factoryCallCount++;
                await Task.CompletedTask;
                return "value1";
            },
            new[] { tag });

        await _cacheService.GetOrCreateAsync(
            "test-key-7b",
            async ct =>
            {
                factoryCallCount++;
                await Task.CompletedTask;
                return "value2";
            },
            new[] { tag });

        // Act - Remove by tag
        await _cacheService.RemoveByTagAsync(tag);

        // Act - Try to get values again
        await _cacheService.GetOrCreateAsync(
            "test-key-7a",
            async ct =>
            {
                factoryCallCount++;
                await Task.CompletedTask;
                return "value1";
            },
            new[] { tag });

        await _cacheService.GetOrCreateAsync(
            "test-key-7b",
            async ct =>
            {
                factoryCallCount++;
                await Task.CompletedTask;
                return "value2";
            },
            new[] { tag });

        // Assert
        factoryCallCount.Should().Be(4, "factory should be called 4 times: 2 initial + 2 after tag removal");
    }

    [Fact]
    public async Task GetOrCreateAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _cacheService.GetOrCreateAsync(
                "test-key-8",
                async ct =>
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Delay(1000, ct);
                    return "value";
                },
                cts.Token);
        });
    }

    [Fact]
    public async Task GetOrCreateAsync_FactoryReturnsNull_ShouldCacheNull()
    {
        // Arrange
        var factoryCallCount = 0;

        // First call - cache miss with null result
        var result1 = await _cacheService.GetOrCreateAsync<string?>(
            "test-key-9",
            async ct =>
            {
                factoryCallCount++;
                await Task.CompletedTask;
                return null;
            });

        // Second call - should return cached null without calling factory
        var result2 = await _cacheService.GetOrCreateAsync<string?>(
            "test-key-9",
            async ct =>
            {
                factoryCallCount++;
                await Task.CompletedTask;
                return "should-not-be-called";
            });

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();
        factoryCallCount.Should().Be(1, "factory should only be called once, null should be cached");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
