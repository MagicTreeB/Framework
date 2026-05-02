using Microsoft.Extensions.Caching.Memory;
using MagicTree.Framework.Idempotency.Interfaces;
using MagicTree.Framework.Idempotency.Models;

namespace MagicTree.Framework.Idempotency.Storage;

/// <summary>
/// In-memory storage for idempotency records (single instance only)
/// </summary>
public class InMemoryIdempotencyStorage : IIdempotencyStorage
{
    private readonly IMemoryCache _cache;
    private readonly object _lock = new();

    public InMemoryIdempotencyStorage(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<IdempotencyRecord?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryGetValue(key, out IdempotencyRecord? record);
        return Task.FromResult(record);
    }

    public Task SetAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = record.ExpiresAt
        };

        lock (_lock)
        {
            _cache.Set(record.Key, record, options);
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryMarkAsProcessingAsync(
        string key,
        string requestMethod,
        string requestPath,
        int expirationHours,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            // Check if already exists or processing
            if (_cache.TryGetValue(key, out IdempotencyRecord? existingRecord))
            {
                if (existingRecord!.IsProcessing)
                {
                    return Task.FromResult(false); // Already processing
                }

                // Already completed, don't mark as processing
                return Task.FromResult(false);
            }

            // Mark as processing
            var processingRecord = new IdempotencyRecord
            {
                Key = key,
                IsProcessing = true,
                RequestMethod = requestMethod,
                RequestPath = requestPath,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(expirationHours)
            };

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = processingRecord.ExpiresAt
            };

            _cache.Set(key, processingRecord, options);
            return Task.FromResult(true);
        }
    }

    public Task RemoveProcessingMarkAsync(string key, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out IdempotencyRecord? record))
            {
                if (record!.IsProcessing)
                {
                    _cache.Remove(key);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var exists = _cache.TryGetValue(key, out _);
        return Task.FromResult(exists);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _cache.Remove(key);
        }

        return Task.CompletedTask;
    }
}
