using MagicTree.Framework.Idempotency.Models;

namespace MagicTree.Framework.Idempotency.Interfaces;

/// <summary>
/// Storage interface for idempotency records
/// </summary>
public interface IIdempotencyStorage
{
    /// <summary>
    /// Get an idempotency record by key
    /// </summary>
    Task<IdempotencyRecord?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Store an idempotency record
    /// </summary>
    Task SetAsync(IdempotencyRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a request as being processed (for conflict detection)
    /// Returns false if already processing
    /// </summary>
    Task<bool> TryMarkAsProcessingAsync(string key, string requestMethod, string requestPath, int expirationHours, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove processing mark (called if request fails)
    /// </summary>
    Task RemoveProcessingMarkAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a key exists
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an idempotency record
    /// </summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
