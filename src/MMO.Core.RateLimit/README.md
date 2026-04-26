# MMO.Core.RateLimit

Flexible rate limiting middleware for ASP.NET Core APIs with support for both in-memory and distributed Redis storage.

## Features

- ✅ **Multiple Storage Options**: In-memory (single instance) or Redis (distributed)
- ✅ **Sliding Window Algorithm**: Accurate rate limiting with time-based windows
- ✅ **Flexible Identifier Types**: Rate limit by IP address, User ID, or Client ID
- ✅ **Endpoint-Specific Rules**: Configure different limits per endpoint
- ✅ **Global Fallback**: Default rate limit for all endpoints
- ✅ **IP Whitelist**: Bypass rate limiting for trusted IPs
- ✅ **Standard Headers**: X-RateLimit-Limit, X-RateLimit-Remaining, X-RateLimit-Reset, Retry-After
- ✅ **Production-Ready**: Thread-safe, atomic operations, proper error handling

## Installation

Add package reference to your API project:

```xml
<ProjectReference Include="..\..\..\Core\MMO.Core.RateLimit\MMO.Core.RateLimit.csproj" />
```

## Configuration

### appsettings.json

```json
{
  "RateLimiting": {
    "Enabled": true,
    "StorageType": "InMemory",
    "RedisConnectionString": "",
    "Global": {
      "Limit": 100,
      "WindowSeconds": 60,
      "Strategy": "SlidingWindow",
      "IdentifierType": "IP"
    },
    "Endpoints": {
      "/api/users/login": {
        "Limit": 5,
        "WindowSeconds": 300,
        "Strategy": "SlidingWindow",
        "IdentifierType": "IP"
      },
      "/api/users/register": {
        "Limit": 3,
        "WindowSeconds": 300,
        "Strategy": "SlidingWindow",
        "IdentifierType": "IP"
      },
      "/connect/token": {
        "Limit": 10,
        "WindowSeconds": 60,
        "Strategy": "SlidingWindow",
        "IdentifierType": "IP"
      }
    },
    "IpWhitelist": [],
    "Headers": {
      "IncludeHeaders": true,
      "LimitHeader": "X-RateLimit-Limit",
      "RemainingHeader": "X-RateLimit-Remaining",
      "ResetHeader": "X-RateLimit-Reset",
      "RetryAfterHeader": "Retry-After"
    }
  }
}
```

### Program.cs

```csharp
using MMO.Core.RateLimit.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register rate limiting services
builder.Services.AddRateLimiting(builder.Configuration);

var app = builder.Build();

// Use rate limiting middleware (BEFORE UseAuthentication)
app.UseRateLimiting();
app.UseAuthentication();
app.UseAuthorization();
```

## Storage Options

### In-Memory Storage (Development/Single Instance)

```json
{
  "RateLimiting": {
    "StorageType": "InMemory"
  }
}
```

- Uses `IMemoryCache` from `Microsoft.Extensions.Caching.Memory`
- Thread-safe with locking mechanism
- Best for development or single-instance deployments
- No external dependencies

### Redis Storage (Production/Distributed)

```json
{
  "RateLimiting": {
    "StorageType": "Redis",
    "RedisConnectionString": "localhost:6379"
  }
}
```

- Uses `StackExchange.Redis` for distributed caching
- Atomic operations (no race conditions)
- Supports multi-instance deployments
- Requires Redis server

## Identifier Types

The middleware extracts identifiers in this priority order:

1. **User ID** - From authenticated user (`context.User.Identity.Name`)
   - Format: `user:{userId}`
   - Best for authenticated endpoints

2. **Client ID** - From `X-Client-Id` header
   - Format: `client:{clientId}`
   - Good for API key-based authentication

3. **IP Address** - From connection or proxy headers
   - Format: `ip:{ipAddress}`
   - Fallback for anonymous requests
   - Handles `X-Forwarded-For`, `X-Real-IP` headers

## Rate Limit Rules

### Global Rule

Applied to all endpoints without specific configuration:

```json
{
  "Global": {
    "Limit": 100,
    "WindowSeconds": 60,
    "Strategy": "SlidingWindow",
    "IdentifierType": "IP"
  }
}
```

### Endpoint-Specific Rules

Override global limits for specific paths:

```json
{
  "Endpoints": {
    "/api/auth/login": {
      "Limit": 5,
      "WindowSeconds": 300
    }
  }
}
```

### Pattern Matching

Use wildcards for multiple endpoints:

```json
{
  "Endpoints": {
    "/api/auth/*": {
      "Limit": 10,
      "WindowSeconds": 60
    }
  }
}
```

## Response Headers

When `IncludeHeaders: true`, the middleware adds:

- **X-RateLimit-Limit**: Maximum requests allowed in window
- **X-RateLimit-Remaining**: Requests remaining in current window
- **X-RateLimit-Reset**: UTC timestamp when window resets

On rate limit exceeded (429 response):

- **Retry-After**: Seconds until the rate limit resets

Example response:

```
HTTP/1.1 429 Too Many Requests
X-RateLimit-Limit: 5
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 2025-12-10T10:35:00Z
Retry-After: 120
Content-Type: application/json

{
  "error": "Rate limit exceeded",
  "message": "Too many requests. Please try again in 120 seconds.",
  "limit": 5,
  "remaining": 0,
  "resetAt": "2025-12-10T10:35:00Z"
}
```

## IP Whitelist

Bypass rate limiting for trusted IPs:

```json
{
  "IpWhitelist": [
    "127.0.0.1",
    "::1",
    "10.0.0.0/8",
    "192.168.1.100"
  ]
}
```

## Testing

### Using PowerShell

```powershell
# Test login endpoint (limit: 5 requests per 300 seconds)
for ($i = 1; $i -le 10; $i++) {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/users/login" `
        -Method POST `
        -Body (@{username="test"; password="test"} | ConvertTo-Json) `
        -ContentType "application/json" `
        -Headers @{"Accept"="*/*"} `
        -SkipHttpErrorCheck
    
    Write-Host "Request $i - Status: $($response.StatusCode)"
    Write-Host "Limit: $($response.Headers['X-RateLimit-Limit'])"
    Write-Host "Remaining: $($response.Headers['X-RateLimit-Remaining'])"
    Write-Host "Reset: $($response.Headers['X-RateLimit-Reset'])"
    
    if ($response.StatusCode -eq 429) {
        Write-Host "Rate limited! Retry-After: $($response.Headers['Retry-After']) seconds"
        break
    }
    
    Start-Sleep -Milliseconds 100
}
```

### Using cURL

```bash
# Test with 10 rapid requests
for i in {1..10}; do
  curl -i -X POST http://localhost:5000/api/users/login \
    -H "Content-Type: application/json" \
    -d '{"username":"test","password":"test"}'
  echo "Request $i"
  sleep 0.1
done
```

## Architecture

```
┌─────────────────────────────────────────────────┐
│            RateLimitMiddleware                  │
│  - Extract identifier (User/Client/IP)          │
│  - Check whitelist                              │
│  - Load endpoint rule or global fallback        │
│  - Call IRateLimitService                       │
│  - Add response headers                         │
│  - Return 429 if exceeded                       │
└─────────────────┬───────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────┐
│          IRateLimitService                      │
│  - RateLimitService (sliding window)            │
│  - CheckRateLimitAsync()                        │
│  - ResetAsync()                                 │
└─────────────────┬───────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────┐
│          IRateLimitStorage                      │
│  ┌───────────────────┬─────────────────────┐   │
│  │  InMemoryStorage  │   RedisStorage      │   │
│  │  (IMemoryCache)   │ (StackExchange.Redis)│  │
│  │  - Thread-safe    │ - Atomic operations │   │
│  │  - Single instance│ - Distributed       │   │
│  └───────────────────┴─────────────────────┘   │
└─────────────────────────────────────────────────┘
```

## Best Practices

1. **Use Redis in Production**: For multi-instance deployments
2. **Place Middleware Early**: Before `UseAuthentication()` to rate limit login attempts
3. **Configure Per-Endpoint**: Stricter limits for sensitive endpoints
4. **Monitor 429 Responses**: Track rate limit violations in logs
5. **Whitelist Health Checks**: Add monitoring IPs to whitelist
6. **Set Reasonable Limits**: Balance security vs usability
7. **Use Sliding Window**: More accurate than fixed window

## Common Configurations

### Development

```json
{
  "Enabled": true,
  "StorageType": "InMemory",
  "Global": { "Limit": 1000, "WindowSeconds": 60 }
}
```

### Production (Auth API)

```json
{
  "Enabled": true,
  "StorageType": "Redis",
  "RedisConnectionString": "redis:6379",
  "Global": { "Limit": 100, "WindowSeconds": 60 },
  "Endpoints": {
    "/api/users/login": { "Limit": 5, "WindowSeconds": 300 },
    "/api/users/register": { "Limit": 3, "WindowSeconds": 300 },
    "/connect/token": { "Limit": 10, "WindowSeconds": 60 }
  },
  "IpWhitelist": ["10.0.0.0/8"]
}
```

## Troubleshooting

### Issue: Rate limit not working

**Solution**: Ensure middleware is registered before `UseAuthentication()`:

```csharp
app.UseRateLimiting();  // ✅ Before authentication
app.UseAuthentication();
```

### Issue: All requests use IP identifier

**Solution**: Ensure JWT middleware extracts user identity correctly:

```csharp
app.UseAuthentication();
app.UseJwtMiddleware();  // Extracts user claims
app.UseRateLimiting();   // Now can use User ID
```

### Issue: Redis connection failed

**Solution**: Verify Redis connection string and ensure Redis is running:

```bash
# Test Redis connection
docker run -p 6379:6379 redis:latest
```

### Issue: 429 errors in health checks

**Solution**: Add health check endpoint to whitelist or exclude pattern

## Future Enhancements

- [ ] Token bucket algorithm support
- [ ] Fixed window algorithm support
- [ ] Distributed lock for critical sections
- [ ] Rate limit by custom claims (roles, organizations)
- [ ] Admin API to view/reset rate limits
- [ ] Metrics and monitoring integration
- [ ] Cost-based rate limiting (weighted requests)

## License

Part of the MMO project - Internal use only
