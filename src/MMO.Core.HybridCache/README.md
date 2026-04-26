# MMO.Core.HybridCache

Standardized wrapper around .NET 10's `HybridCache` for consistent two-tier caching across all microservices.

## Overview

HybridCache combines in-memory (L1) and distributed (L2) caching with automatic stampede protection, providing a simple API for read-heavy query operations.

### Key Features
- ✅ **Two-tier caching**: L1 (IMemoryCache) + L2 (Redis)
- ✅ **Stampede protection**: Prevents concurrent duplicate database queries
- ✅ **Type-safe APIs**: Generic methods with compile-time safety
- ✅ **Tag-based invalidation**: Group and invalidate related cache entries
- ✅ **Configurable per-entry**: Different TTL settings per cache key

### Performance
- **L1 (Memory)**: ~1-10 µs (microseconds)
- **L2 (Redis)**: ~1-5 ms (milliseconds)
- **Database**: ~10-100 ms (milliseconds)

## Installation

Add project reference:
```xml
<ProjectReference Include="..\..\..\Core\MMO.Core.HybridCache\MMO.Core.HybridCache.csproj" />
```

## Configuration

**appsettings.json:**
```json
{
  "HybridCache": {
    "MaximumPayloadBytes": 1048576,
    "MaximumKeyLength": 512,
    "DefaultExpirationMinutes": 5,
    "LocalCacheExpirationMinutes": 1
  }
}
```

## Usage

### Program.cs Setup
```csharp
using MMO.Core.HybridCache.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register HybridCache
builder.Services.AddHybridCacheService(builder.Configuration);

// Register Redis (required for L2 cache)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
```

### Query Handler Example
```csharp
using MMO.Core.HybridCache.Services;

public class GetUserQueryHandler : IHandler<GetUserQuery, UserDto>
{
    private readonly CacheService _cache;
    private readonly IUserRepository _repository;
    
    public GetUserQueryHandler(CacheService cache, IUserRepository repository)
    {
        _cache = cache;
        _repository = repository;
    }
    
    public async Task<UserDto> Handle(GetUserQuery query, CancellationToken ct)
    {
        return await _cache.GetOrCreateAsync(
            $"user:{query.UserId}",
            async cancel => 
            {
                var user = await _repository.GetByIdAsync(query.UserId, cancel);
                return user?.ToDto();
            },
            TimeSpan.FromMinutes(10), // L2 expiration
            TimeSpan.FromMinutes(2),  // L1 expiration
            ct
        );
    }
}
```

## CacheService API

### GetOrCreateAsync (Default Expiration)
```csharp
Task<TResult> GetOrCreateAsync<TResult>(
    string key,
    Func<CancellationToken, Task<TResult>> factory,
    CancellationToken ct = default)
```

### GetOrCreateAsync (Custom Expiration)
```csharp
Task<TResult> GetOrCreateAsync<TResult>(
    string key,
    Func<CancellationToken, Task<TResult>> factory,
    TimeSpan? expiration,
    TimeSpan? localExpiration = null,
    CancellationToken ct = default)
```

### GetOrCreateAsync (With Tags)
```csharp
Task<TResult> GetOrCreateAsync<TResult>(
    string key,
    Func<CancellationToken, Task<TResult>> factory,
    string[] tags,
    TimeSpan? expiration = null,
    TimeSpan? localExpiration = null,
    CancellationToken ct = default)
```

### SetAsync
```csharp
Task SetAsync<TValue>(
    string key,
    TValue value,
    TimeSpan? expiration = null,
    CancellationToken ct = default)
```

### RemoveAsync
```csharp
Task RemoveAsync(string key, CancellationToken ct = default)
```

### RemoveByTagAsync
```csharp
Task RemoveByTagAsync(string tag, CancellationToken ct = default)
```

## Common Patterns

### 1. User Profile Caching
```csharp
public async Task<UserDto> GetUserAsync(Guid userId, CancellationToken ct)
{
    return await _cache.GetOrCreateAsync(
        $"user:{userId}",
        async cancel => await _repository.GetUserDtoAsync(userId, cancel),
        TimeSpan.FromMinutes(10), // Cache 10 minutes
        TimeSpan.FromMinutes(2),  // Local cache 2 minutes
        ct
    );
}
```

### 2. Product Catalog Caching
```csharp
public async Task<ProductDto> GetProductAsync(Guid productId, CancellationToken ct)
{
    return await _cache.GetOrCreateAsync(
        $"product:{productId}",
        async cancel => await _repository.GetProductDtoAsync(productId, cancel),
        TimeSpan.FromHours(1),   // Cache 1 hour
        TimeSpan.FromMinutes(5), // Local cache 5 minutes
        ct
    );
}
```

### 3. List Caching
```csharp
public async Task<List<CategoryDto>> GetAllCategoriesAsync(CancellationToken ct)
{
    return await _cache.GetOrCreateAsync(
        "categories:all",
        async cancel => await _repository.GetAllCategoriesDtoAsync(cancel),
        TimeSpan.FromHours(24), // Cache 24 hours
        TimeSpan.FromHours(1),  // Local cache 1 hour
        ct
    );
}
```

### 4. Cache Invalidation on Update
```csharp
public class UpdateUserCommandHandler : IHandler<UpdateUserCommand, UserDto>
{
    private readonly CacheService _cache;
    private readonly IUserRepository _repository;
    
    public async Task<IResult<UserDto>> Handle(UpdateUserCommand command)
    {
        var user = await _repository.GetByIdAsync(command.UserId);
        user.Update(command.FirstName, command.LastName);
        await _repository.UpdateAsync(user);
        
        // Invalidate cache after update
        await _cache.RemoveAsync($"user:{command.UserId}");
        
        return Result.Ok(user.ToDto());
    }
}
```

### 5. Tag-Based Invalidation
```csharp
// Cache with tags
await _cache.GetOrCreateAsync(
    $"user:{userId}:profile",
    async cancel => await GetUserProfileAsync(userId, cancel),
    tags: new[] { $"user:{userId}" }
);

await _cache.GetOrCreateAsync(
    $"user:{userId}:orders",
    async cancel => await GetUserOrdersAsync(userId, cancel),
    tags: new[] { $"user:{userId}" }
);

// Invalidate all user-related caches
await _cache.RemoveByTagAsync($"user:{userId}");
```

## When to Use

### ✅ DO Use For
- Read-heavy query operations (GET endpoints)
- User profiles, product catalogs, configuration data
- Aggregated statistics and reports
- Frequently accessed reference data
- Query handlers in CQRS pattern

### ❌ DON'T Use For
- Write operations (POST/PUT/PATCH/DELETE)
- Real-time data requiring millisecond freshness
- Session state (use IDistributedCache directly)
- Rate limiting counters (use MMO.Core.RateLimit)
- Idempotency tracking (use MMO.Core.Idempotency)

## Integration with Other Packages

| Package | Purpose | Storage | Use Case |
|---------|---------|---------|----------|
| **MMO.Core.HybridCache** | Query result caching | InMemory + Redis | Read-heavy queries |
| **MMO.Core.RateLimit** | Request throttling | InMemory or Redis | API protection |
| **MMO.Core.Idempotency** | Duplicate prevention | InMemory or Redis | Write operations |

### Shared Redis Connection
All three packages can share the same Redis connection:
```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddIdempotency(builder.Configuration);
builder.Services.AddHybridCacheService(builder.Configuration);
```

## Best Practices

1. ✅ **Cache at Query Handler level** - Not in repositories or domain layer
2. ✅ **Use appropriate expiration times** - Longer for static data, shorter for dynamic
3. ✅ **Invalidate on mutations** - Clear cache after CREATE/UPDATE/DELETE
4. ✅ **Use tag-based invalidation** - For related entries
5. ✅ **Monitor cache hit rates** - Track effectiveness in production
6. ✅ **Handle null results** - Factory should return null for not-found cases
7. ✅ **Use CancellationToken** - Support cancellation for long-running queries

## Recommended Expirations

- **Static reference data**: L1=1hr, L2=24hr
- **User profiles**: L1=2min, L2=10min
- **Product catalogs**: L1=5min, L2=1hr
- **Aggregated stats**: L1=1min, L2=5min

## Dependencies

- Microsoft.Extensions.Caching.Hybrid (10.0.0)
- Microsoft.Extensions.Caching.StackExchangeRedis (10.0.0)
- StackExchange.Redis (2.9.32)
- Microsoft.Extensions.Configuration.Binder (10.0.0)

## Architecture

See `.github/architecture-diagram.md` for visual representation of how HybridCache integrates with the CQRS pattern.

## Related Documentation

- [Project Architecture](../../.github/copilot-instructions.md)
- [MMO.Core.RateLimit Package](../MMO.Core.RateLimit/README.md)
- [MMO.Core.Idempotency Package](../MMO.Core.Idempotency/README.md)
