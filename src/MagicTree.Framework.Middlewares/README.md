# MagicTree.Framework.Middlewares

Shared middleware components for all microservices in the MMO project.

## Included Middlewares

### 1. JwtMiddleware - JWT Token Parsing

**Purpose:** Automatically extracts JWT claims and makes them easily accessible throughout the request pipeline.

**Location:** `Jwt/`

**Usage:**
```csharp
using MagicTree.Framework.Middlewares.Jwt;

app.UseAuthentication();
app.UseJwtMiddleware(); // Add after UseAuthentication
app.UseAuthorization();
```

**Features:**
- Automatic JWT claim extraction when user is authenticated
- Null-safe: Returns `null` when user is not authenticated
- Cached: Stores `JwtInfo` in `HttpContext.Items` for single-parse-per-request
- Rich extension methods: `GetJwtInfo()`, `GetUserId()`, `GetUsername()`, `GetUserEmail()`, `GetUserRoles()`

**JwtInfo Model:**
```csharp
public class JwtInfo
{
    public string UserId { get; set; }              // From "sub" or NameIdentifier claim
    public string? Username { get; set; }            // From "name" claim
    public string? Email { get; set; }               // From "email" claim
    public List<string> Roles { get; set; }          // From Role claims
    public Dictionary<string, string> Claims { get; set; }  // All token claims
}
```

---

### 2. AutoSaveChanges - Automatic DbContext Persistence

**Purpose:** Automatically saves DbContext changes after successful POST, PUT, PATCH, and DELETE requests.

**Location:** `SaveChange/`

**Setup:**
```csharp
using MagicTree.Framework.Middlewares.SaveChange;

// 1. Register services
builder.Services.AddAutoSaveChanges();

// 2. Configure DbContext with tracker interceptor
builder.Services.AddDbContext<YourDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
    var tracker = sp.GetRequiredService<IDbContextTracker>();
    options.AddInterceptors(new DbContextTrackerInterceptor(tracker));
});

// 3. Register middleware BEFORE endpoints
app.UseAutoSaveChanges();
app.MapAllEndpoints("YourApi.ApiEndpoints");
```

**How It Works:**
1. Middleware executes the request pipeline
2. Only saves if response status is 200-299 (success)
3. Only saves for POST, PUT, PATCH, DELETE methods (mutating operations)
4. Checks if DbContext has pending changes via `ChangeTracker`
5. Calls `SaveChangesAsync()` on all tracked DbContexts

**Benefits:**
- ✅ Eliminates boilerplate `SaveChangesAsync()` calls
- ✅ Consistent persistence across all mutating operations
- ✅ Safe: Only saves on successful responses
- ✅ Flexible: Works with multiple DbContexts
- ✅ Performant: Only saves when there are actual changes

---

### 3. NullResponseMiddleware - Null Response Handler

**Purpose:** Intercepts null or empty API responses and returns appropriate HTTP status codes instead of sending null data.

**Location:** `NullResponse/`

**Usage:**
```csharp
using MagicTree.Framework.Middlewares.NullResponse;

app.UseRouting();
app.UseNullResponseHandler(); // Default configuration
app.MapAllEndpoints("YourApi.ApiEndpoints");

// Or with custom configuration
app.UseNullResponseHandler(options =>
{
    options.GetStatusCode = StatusCodes.Status404NotFound;
    options.DeleteStatusCode = StatusCodes.Status204NoContent;
    options.ErrorCode = "NOT_FOUND";
    options.ErrorMessage = "Resource not found.";
});
```

**Default Behavior:**

| HTTP Method | Null Response Returns | Explanation |
|-------------|----------------------|-------------|
| GET | 404 Not Found | Resource doesn't exist |
| DELETE | 204 No Content | Resource deleted successfully |
| POST/PUT/PATCH | 404 Not Found | Target resource not found |
| Others | 404 Not Found | Default fallback |

**Features:**
- ✅ **Automatic Null Detection**: Identifies null, empty, or `{}` JSON responses
- ✅ **HTTP Method-Aware**: Returns different status codes based on request method
- ✅ **Configurable Behavior**: Customize status codes and error messages
- ✅ **Standard Error Format**: Returns consistent JSON error responses
- ✅ **Minimal Overhead**: Only processes successful (2xx) responses

**Example:**

Before (handler returns null):
```csharp
public async Task<UserDto?> Handle(GetUserQuery query, CancellationToken ct)
{
    var user = await _repository.GetByIdAsync(query.UserId, ct);
    return user?.ToDto(); // Returns null if not found
}

// API Response: 200 OK, Body: null
```

After (middleware intercepts):
```csharp
// Same handler code
public async Task<UserDto?> Handle(GetUserQuery query, CancellationToken ct)
{
    var user = await _repository.GetByIdAsync(query.UserId, ct);
    return user?.ToDto(); // Returns null if not found
}

// API Response: 404 Not Found
// Body: { "error": "RESOURCE_NOT_FOUND", "message": "...", "timestamp": "..." }
```

---

## Middleware Pipeline Order

**Recommended Order:**
```csharp
var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiting();          // 1. Rate limiting (before routing)
app.UseIdempotency();           // 2. Idempotency (before routing)
app.UseRouting();
app.UseNullResponseHandler();   // 3. Null response handler (after routing)
app.UseAuthentication();
app.UseAuthorization();
app.UseJwtMiddleware();         // 4. JWT parsing (after auth)
app.UseAutoSaveChanges();       // 5. Auto-save (before endpoints)
app.MapAllEndpoints();
```

## Integration with Other Packages

Works alongside other MagicTree.Framework packages:

| Package | Purpose | Middleware Order |
|---------|---------|------------------|
| **MagicTree.Framework.RateLimit** | Request throttling | 1st (before routing) |
| **MagicTree.Framework.Idempotency** | Duplicate prevention | 2nd (before routing) |
| **MagicTree.Framework.Middlewares** (NullResponse) | Null handling | 3rd (after routing) |
| **MagicTree.Framework.Middlewares** (JwtMiddleware) | JWT parsing | 4th (after auth) |
| **MagicTree.Framework.Middlewares** (AutoSaveChanges) | EF Core auto-save | 5th (before endpoints) |
| **MagicTree.Framework.Exceptions** | Global exception handler | After CORS, before auth |
| **MagicTree.Framework.HybridCache** | Query result caching | N/A (service, not middleware) |

## Dependencies

- Microsoft.AspNetCore.App (framework reference)
- Microsoft.EntityFrameworkCore (for AutoSaveChanges)
- System.Text.Json (for NullResponse)

## Documentation

Each middleware has its own README:
- [JwtMiddleware](Jwt/README.md) - JWT token parsing
- [AutoSaveChanges](SaveChange/README.md) - Automatic DbContext persistence
- [NullResponseMiddleware](NullResponse/README.md) - Null response handling

## Version History

- **v1.0.0**: Initial implementation with JwtMiddleware and AutoSaveChanges
- **v1.1.0** (Dec 14, 2025): Added NullResponseMiddleware
