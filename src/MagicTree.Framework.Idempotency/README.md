# MagicTree.Framework.Idempotency

Idempotency middleware for ASP.NET Core APIs to prevent duplicate operations from being processed multiple times. Ensures that retrying the same request with the same idempotency key produces the same result without side effects.

## Overview

This package provides a complete idempotency solution with:
- ✅ **X-Idempotency-Key header validation** (GUID format required)
- ✅ **Duplicate request prevention** - Cached responses for identical requests
- ✅ **Conflict detection** - Returns 409 when same request is being processed concurrently
- ✅ **Response caching** - Stores successful responses (2xx) with configurable expiration
- ✅ **Dual storage options** - InMemory (single instance) or Redis (distributed)
- ✅ **Endpoint filtering** - Apply to specific endpoints or patterns
- ✅ **HTTP method filtering** - Default: POST, PUT, PATCH, DELETE (mutating operations)
- ✅ **Pattern matching** - Wildcard support for endpoint groups (`/api/payments/*`)
- ✅ **Standard headers** - X-Idempotency-Replayed-At timestamp for cached responses

## When to Use Idempotency

Idempotency is critical for:
- **User registration** - Prevent duplicate accounts from double-clicks
- **Payment processing** - Avoid charging customers twice
- **Order creation** - Prevent duplicate orders from network retries
- **Data mutations** - Ensure operations are applied exactly once

## Installation

Add project reference to your API:

```xml
<ProjectReference Include="..\..\..\Core\MagicTree.Framework.Idempotency\MagicTree.Framework.Idempotency.csproj" />
```

## Configuration

### appsettings.json

```json
{
  "Idempotency": {
    "Enabled": true,
    "StorageType": "InMemory",
    "RedisConnectionString": "",
    "ExpirationHours": 24,
    "HttpMethods": ["POST", "PUT", "PATCH", "DELETE"],
    "Endpoints": [
      "/api/users/register",
      "/api/orders",
      "/api/payments/*"
    ],
    "HeaderName": "X-Idempotency-Key",
    "IncludeTimestampHeader": true,
    "TimestampHeaderName": "X-Idempotency-Replayed-At"
  }
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | bool | `true` | Master switch to enable/disable idempotency |
| `StorageType` | string | `"InMemory"` | Storage backend: `InMemory` or `Redis` |
| `RedisConnectionString` | string | `""` | Redis connection string (e.g., `redis:6379,password=xxx`) |
| `ExpirationHours` | int | `24` | How long to cache responses (in hours) |
| `HttpMethods` | string[] | `["POST", "PUT", "PATCH", "DELETE"]` | HTTP methods to apply idempotency to |
| `Endpoints` | string[] | `[]` | Specific endpoints to protect (supports wildcards) |
| `HeaderName` | string | `"X-Idempotency-Key"` | Header name for idempotency key |
| `IncludeTimestampHeader` | bool | `true` | Add timestamp header to replayed responses |
| `TimestampHeaderName` | string | `"X-Idempotency-Replayed-At"` | Name of timestamp header |

## Usage

### Program.cs Setup

```csharp
using MagicTree.Framework.Idempotency.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Option 1: Load from appsettings.json
builder.Services.AddIdempotency(builder.Configuration);

// Option 2: Programmatic configuration
builder.Services.AddIdempotency(options =>
{
    options.Enabled = true;
    options.StorageType = "Redis";
    options.RedisConnectionString = "localhost:6379";
    options.ExpirationHours = 48;
    options.Endpoints.Add("/api/users/register");
    options.Endpoints.Add("/api/payments/*");
});

var app = builder.Build();

// Add middleware AFTER rate limiting, BEFORE authentication
app.UseRateLimiting();      // Optional: Rate limiting first
app.UseIdempotency();       // Idempotency second
app.UseAuthentication();    // Authentication third
app.UseAuthorization();

app.MapAllEndpoints("YourApi.ApiEndpoints");
app.Run();
```

### Client Usage

Clients must generate a unique GUID and send it in the `X-Idempotency-Key` header:

**cURL Example:**
```bash
# Generate a GUID
IDEMPOTENCY_KEY=$(uuidgen)

# First request - creates resource
curl -X POST https://api.example.com/api/users/register \
  -H "Content-Type: application/json" \
  -H "X-Idempotency-Key: $IDEMPOTENCY_KEY" \
  -d '{"username": "testuser@example.com", "password": "Pass123!"}'

# Response: 201 Created

# Second request (retry) - returns cached response
curl -X POST https://api.example.com/api/users/register \
  -H "Content-Type: application/json" \
  -H "X-Idempotency-Key: $IDEMPOTENCY_KEY" \
  -d '{"username": "testuser@example.com", "password": "Pass123!"}'

# Response: 201 Created (cached)
# Headers: X-Idempotency-Replayed-At: 2025-12-10T10:30:00Z
```

**JavaScript Example:**
```javascript
// Generate idempotency key (UUID v4)
function generateIdempotencyKey() {
  return crypto.randomUUID();
}

// Make idempotent request
async function createUser(userData) {
  const idempotencyKey = generateIdempotencyKey();
  
  const response = await fetch('https://api.example.com/api/users/register', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Idempotency-Key': idempotencyKey
    },
    body: JSON.stringify(userData)
  });
  
  // Check if response was replayed from cache
  const replayedAt = response.headers.get('X-Idempotency-Replayed-At');
  if (replayedAt) {
    console.log('This is a cached response from:', replayedAt);
  }
  
  return await response.json();
}
```

**C# Example:**
```csharp
using System.Net.Http;
using System.Text.Json;

var httpClient = new HttpClient();
var idempotencyKey = Guid.NewGuid().ToString();

var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/api/users/register")
{
    Content = JsonContent.Create(new 
    { 
        Username = "testuser@example.com", 
        Password = "Pass123!" 
    })
};

request.Headers.Add("X-Idempotency-Key", idempotencyKey);

var response = await httpClient.SendAsync(request);

// Check if cached
if (response.Headers.TryGetValues("X-Idempotency-Replayed-At", out var values))
{
    Console.WriteLine($"Cached response from: {values.First()}");
}
```

## Request Flow

```
┌─────────────────────────────────────────────────────────────┐
│ Client sends POST /api/users/register                       │
│ Header: X-Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000 │
└───────────────────┬─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│ Idempotency Middleware                                      │
│ 1. Validate key format (must be valid GUID)                │
│ 2. Check if HTTP method matches (POST/PUT/PATCH/DELETE)    │
│ 3. Check if endpoint matches configured patterns           │
└───────────────────┬─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│ Check Storage for Existing Record                           │
└───────────────────┬─────────────────────────────────────────┘
                    │
        ┌───────────┴───────────┐
        │                       │
        ▼                       ▼
┌──────────────────┐    ┌──────────────────┐
│ Record EXISTS    │    │ Record NOT FOUND │
│ IsProcessing=true│    └────────┬─────────┘
└────────┬─────────┘             │
         │                       ▼
         │              ┌──────────────────┐
         │              │ Mark as Processing│
         │              │ (atomic SET NX)   │
         │              └────────┬─────────┘
         │                       │
         │              ┌────────┴─────────┐
         │              │ Request Succeeds │    Request Fails
         │              │ (2xx status)     │    (4xx/5xx)
         │              └────────┬─────────┘         │
         │                       │                   │
         │              ┌────────▼─────────┐         │
         │              │ Cache Response   │         │
         │              │ + Headers + Body │         │
         │              │ Set Expiration   │         │
         │              └────────┬─────────┘         │
         │                       │                   │
         │                       └───────────────────┤
         │                                           │
         ▼                                           ▼
┌──────────────────┐                    ┌──────────────────┐
│ Return 409       │                    │ Remove Processing│
│ Conflict         │                    │ Mark             │
│ "Already         │                    └────────┬─────────┘
│  Processing"     │                             │
└──────────────────┘                             ▼
                                        ┌──────────────────┐
                                        │ Return Original  │
                                        │ Response         │
                                        └──────────────────┘
         │
         ▼
┌──────────────────┐
│ Replay Cached    │
│ Response         │
│ + Add Header:    │
│ X-Idempotency-   │
│ Replayed-At      │
└──────────────────┘
```

## Response Codes

| Status | Meaning | When |
|--------|---------|------|
| **200/201** | Success (Cached) | Request processed previously, returning cached response |
| **400** | Bad Request | Invalid idempotency key format (not a GUID) |
| **409** | Conflict | Same request is currently being processed by another thread/instance |
| **Original** | Pass-through | Request not cached yet, processing normally |

## Storage Options

### InMemory Storage

**Best for:** Development, single-instance deployments, testing

**Pros:**
- ✅ No external dependencies
- ✅ Fast (in-process memory access)
- ✅ Simple setup

**Cons:**
- ❌ Lost on app restart
- ❌ Not shared across multiple instances
- ❌ Limited by server memory

**Configuration:**
```json
{
  "Idempotency": {
    "StorageType": "InMemory"
  }
}
```

### Redis Storage

**Best for:** Production, distributed systems, multi-instance deployments

**Pros:**
- ✅ Persistent across restarts
- ✅ Shared across all API instances
- ✅ Scalable and reliable
- ✅ Can share connection with rate limiting

**Cons:**
- ❌ Requires Redis server
- ❌ Network latency overhead

**Configuration:**
```json
{
  "Idempotency": {
    "StorageType": "Redis",
    "RedisConnectionString": "redis-server:6379,password=your-password"
  }
}
```

**Sharing Redis with Rate Limiting:**
The package automatically detects if `IConnectionMultiplexer` is already registered (e.g., by `MagicTree.Framework.RateLimit`) and reuses the connection:

```csharp
// Rate limiting registers Redis first
builder.Services.AddRateLimiting(builder.Configuration);

// Idempotency reuses the same connection
builder.Services.AddIdempotency(builder.Configuration);
```

## Testing

### PowerShell Test Script

Use the included test script to verify idempotency:

```powershell
# Run from Apis/Auth directory
.\test-idempotency.ps1
```

This script:
1. ✅ Sends 3 identical requests with same idempotency key
2. ✅ Verifies first request creates resource
3. ✅ Verifies subsequent requests return cached response with `X-Idempotency-Replayed-At` header
4. ✅ Tests request without idempotency key (should process normally)
5. ✅ Tests invalid key format (should return 400)

### Manual Testing

**Test 1: Duplicate Request Prevention**
```powershell
$key = [Guid]::NewGuid().ToString()

# First request
Invoke-WebRequest -Uri "https://localhost:7001/api/users/register" `
  -Method POST `
  -Body '{"username":"test@example.com","password":"Pass123!"}' `
  -ContentType "application/json" `
  -Headers @{"X-Idempotency-Key"=$key} `
  -SkipCertificateCheck

# Second request (should be cached)
Invoke-WebRequest -Uri "https://localhost:7001/api/users/register" `
  -Method POST `
  -Body '{"username":"test@example.com","password":"Pass123!"}' `
  -ContentType "application/json" `
  -Headers @{"X-Idempotency-Key"=$key} `
  -SkipCertificateCheck
```

**Test 2: Concurrent Request Conflict**
```powershell
$key = [Guid]::NewGuid().ToString()

# Send 2 requests simultaneously (one should get 409)
$job1 = Start-Job -ScriptBlock {
    param($k)
    Invoke-WebRequest -Uri "https://localhost:7001/api/users/register" `
      -Method POST `
      -Body '{"username":"test1@example.com","password":"Pass123!"}' `
      -ContentType "application/json" `
      -Headers @{"X-Idempotency-Key"=$k} `
      -SkipCertificateCheck
} -ArgumentList $key

$job2 = Start-Job -ScriptBlock {
    param($k)
    Invoke-WebRequest -Uri "https://localhost:7001/api/users/register" `
      -Method POST `
      -Body '{"username":"test1@example.com","password":"Pass123!"}' `
      -ContentType "application/json" `
      -Headers @{"X-Idempotency-Key"=$k} `
      -SkipCertificateCheck
} -ArgumentList $key

Wait-Job $job1, $job2
Receive-Job $job1, $job2
```

## Best Practices

### Key Generation
- ✅ **DO** generate a new UUID/GUID for each unique operation
- ✅ **DO** use UUIDv4 or equivalent (cryptographically random)
- ❌ **DON'T** reuse keys across different operations
- ❌ **DON'T** use sequential or predictable keys

### Endpoint Selection
- ✅ **DO** apply to endpoints that mutate data (POST/PUT/PATCH/DELETE)
- ✅ **DO** protect critical operations (payments, orders, registrations)
- ❌ **DON'T** apply to GET requests (they should be idempotent by nature)
- ❌ **DON'T** apply to endpoints that require different results on retry

### Expiration
- ✅ **DO** set expiration longer than max expected retry window (24 hours default)
- ✅ **DO** consider business requirements (regulatory retention, audit logs)
- ❌ **DON'T** set too short (client may retry after expiration)
- ❌ **DON'T** set too long (wastes storage space)

### Error Handling
- ✅ **DO** handle 409 Conflict on client (wait and retry with same key)
- ✅ **DO** handle 400 Bad Request for invalid keys (regenerate key)
- ✅ **DO** log idempotency key for debugging failed requests
- ❌ **DON'T** generate new key on conflict (defeats the purpose)

### Storage Selection
- ✅ **DO** use InMemory for development and testing
- ✅ **DO** use Redis for production and multi-instance deployments
- ✅ **DO** share Redis connection with rate limiting if both are used
- ❌ **DON'T** use InMemory in production with multiple instances

## Troubleshooting

### Issue: "X-Idempotency-Key header is missing or invalid"

**Cause:** Client didn't send header or sent non-GUID value

**Fix:**
```csharp
// Correct: Valid GUID
var key = Guid.NewGuid().ToString();
request.Headers.Add("X-Idempotency-Key", key);

// Wrong: Invalid format
request.Headers.Add("X-Idempotency-Key", "my-custom-key");  // ❌
```

### Issue: Always getting 409 Conflict

**Cause:** Processing mark not removed after failed request

**Fix:** Check logs for exceptions in endpoint handlers. Middleware should remove mark on completion, but unhandled exceptions may prevent cleanup.

**Solution:**
```csharp
// Ensure handlers don't throw unhandled exceptions
public async Task<IResult<UserDto>> Handle(CreateUserCommand command)
{
    try 
    {
        // Your logic
        return Result.Ok(user);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create user");
        return Result.Fail<UserDto>("User creation failed");
    }
}
```

### Issue: Cached response returned for changed request body

**Cause:** Same idempotency key used with different request data

**Fix:** Generate a new key for each unique operation attempt. Idempotency keys should be unique per operation, not per endpoint.

### Issue: Redis connection errors

**Cause:** Invalid connection string or Redis server not accessible

**Fix:**
1. Verify Redis is running: `redis-cli ping` (should return "PONG")
2. Check connection string format: `host:port,password=xxx`
3. Test connection manually:
```powershell
$redis = [StackExchange.Redis.ConnectionMultiplexer]::Connect("localhost:6379")
$redis.GetDatabase().Ping()
```

### Issue: Responses not cached

**Cause:** Endpoint not matching configured patterns or HTTP method not included

**Fix:**
1. Check `Endpoints` configuration includes your endpoint (or use wildcard `/*`)
2. Verify `HttpMethods` includes your request method (default: POST, PUT, PATCH, DELETE)
3. Enable logging to see middleware decisions:
```json
{
  "Logging": {
    "LogLevel": {
      "MagicTree.Framework.Idempotency": "Debug"
    }
  }
}
```

## Architecture

### Package Structure
```
MagicTree.Framework.Idempotency/
├── Options/
│   └── IdempotencyOptions.cs          # Configuration model
├── Models/
│   └── IdempotencyRecord.cs           # Cached response record
├── Interfaces/
│   └── IIdempotencyStorage.cs         # Storage abstraction
├── Storage/
│   ├── InMemoryIdempotencyStorage.cs  # IMemoryCache implementation
│   └── RedisIdempotencyStorage.cs     # Redis implementation
├── Middlewares/
│   └── IdempotencyMiddleware.cs       # Request interception
└── Extensions/
    └── IdempotencyExtensions.cs       # DI registration
```

### Dependencies
- **Microsoft.Extensions.Caching.Memory** (10.0.0) - InMemory storage
- **StackExchange.Redis** (2.9.32) - Redis storage
- **Microsoft.AspNetCore.Http.Abstractions** (2.2.0) - Middleware support
- **Microsoft.Extensions.Configuration.Binder** (10.0.0) - Configuration binding
- **Microsoft.Extensions.Options** (10.0.0) - Options pattern
- **Microsoft.Extensions.DependencyInjection.Abstractions** (10.0.0) - DI registration
- **System.Text.Json** (10.0.1) - Response serialization

## Related Documentation

- [Auth API Rate Limiting](../../Apis/Auth/RATE-LIMITING.md)
- [Auth API Testing Guide](../../Apis/Auth/TESTING-GUIDE.md)
- [MagicTree.Framework.RateLimit Package](../MagicTree.Framework.RateLimit/README.md)
- [Project Architecture](../../.github/copilot-instructions.md)

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
