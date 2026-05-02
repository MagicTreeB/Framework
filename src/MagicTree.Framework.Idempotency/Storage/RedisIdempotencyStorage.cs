using MagicTree.Framework.Idempotency.Interfaces;
using MagicTree.Framework.Idempotency.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace MagicTree.Framework.Idempotency.Storage;

/// <summary>
/// Redis storage for idempotency records (distributed)
/// </summary>
public class RedisIdempotencyStorage : IIdempotencyStorage
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;

    public RedisIdempotencyStorage(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _database = redis.GetDatabase();
    }

    public async Task<IdempotencyRecord?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(key);
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<IdempotencyRecord>(value.ToString());
    }

    public async Task SetAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(record);
        var expiry = record.ExpiresAt - DateTimeOffset.UtcNow;

        await _database.StringSetAsync(record.Key, json, expiry);
    }

    public async Task<bool> TryMarkAsProcessingAsync(
        string key,
        string requestMethod,
        string requestPath,
        int expirationHours,
        CancellationToken cancellationToken = default)
    {
        // Check if key already exists
        var exists = await _database.KeyExistsAsync(key);
        if (exists)
        {
            var existingJson = await _database.StringGetAsync(key);
            if (!existingJson.IsNullOrEmpty)
            {
                var existing = JsonSerializer.Deserialize<IdempotencyRecord>(existingJson.ToString());
                if (existing?.IsProcessing == true)
                {
                    return false; // Already processing
                }

                // Already completed
                return false;
            }
        }

        // Create processing record
        var processingRecord = new IdempotencyRecord
        {
            Key = key,
            IsProcessing = true,
            RequestMethod = requestMethod,
            RequestPath = requestPath,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(expirationHours)
        };

        var json = JsonSerializer.Serialize(processingRecord);
        var expiry = TimeSpan.FromHours(expirationHours);

        // Use SET NX (only set if not exists) for atomic operation
        var wasSet = await _database.StringSetAsync(key, json, expiry, When.NotExists);
        return wasSet;
    }

    public async Task RemoveProcessingMarkAsync(string key, CancellationToken cancellationToken = default)
    {
        var existingJson = await _database.StringGetAsync(key);
        if (!existingJson.IsNullOrEmpty)
        {
            var existing = JsonSerializer.Deserialize<IdempotencyRecord>(existingJson.ToString());
            if (existing?.IsProcessing == true)
            {
                await _database.KeyDeleteAsync(key);
            }
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _database.KeyExistsAsync(key);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(key);
    }
}
