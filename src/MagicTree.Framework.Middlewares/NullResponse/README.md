# NullResponse Middleware

## Overview

The **NullResponseMiddleware** intercepts API responses and handles null or empty responses by returning appropriate HTTP status codes instead of sending null data to clients. This improves API consistency and follows RESTful best practices.

## Features

- ✅ **Automatic Null Detection**: Identifies null, empty, or `{}` JSON responses
- ✅ **HTTP Method-Aware**: Returns different status codes based on request method
- ✅ **Configurable Behavior**: Customize status codes and error messages
- ✅ **Standard Error Format**: Returns consistent JSON error responses
- ✅ **Minimal Overhead**: Only processes successful (2xx) responses

## Default Behavior

| HTTP Method | Null Response Returns | Explanation |
|-------------|----------------------|-------------|
| GET | 404 Not Found | Resource doesn't exist |
| DELETE | 204 No Content | Resource deleted successfully |
| POST/PUT/PATCH | 404 Not Found | Target resource not found |
| Others | 404 Not Found | Default fallback |

## Installation

### 1. Register Middleware in Program.cs

```csharp
using MagicTree.Framework.Middlewares.NullResponse;

var app = builder.Build();

// Add middleware AFTER routing, BEFORE endpoints
app.UseRouting();
app.UseNullResponseHandler(); // Default configuration
app.MapAllEndpoints("YourApi.ApiEndpoints");
```

### 2. Custom Configuration

```csharp
// Option 1: Configure inline
app.UseNullResponseHandler(options =>
{
    options.GetStatusCode = StatusCodes.Status404NotFound;
    options.DeleteStatusCode = StatusCodes.Status204NoContent;
    options.MutationStatusCode = StatusCodes.Status404NotFound;
    options.IncludeMessage = true;
    options.ErrorCode = "NOT_FOUND";
    options.ErrorMessage = "Resource not found.";
});

// Option 2: Pass options object
var options = new NullResponseOptions
{
    GetStatusCode = StatusCodes.Status404NotFound,
    IncludeMessage = false // No error message in response body
};
app.UseNullResponseHandler(options);
```

## Configuration Options

```csharp
public class NullResponseOptions
{
    /// <summary>
    /// Status code for GET requests with null response (default: 404)
    /// </summary>
    public int GetStatusCode { get; set; } = StatusCodes.Status404NotFound;

    /// <summary>
    /// Status code for DELETE requests with null response (default: 204)
    /// </summary>
    public int DeleteStatusCode { get; set; } = StatusCodes.Status204NoContent;

    /// <summary>
    /// Status code for POST/PUT/PATCH with null response (default: 404)
    /// </summary>
    public int MutationStatusCode { get; set; } = StatusCodes.Status404NotFound;

    /// <summary>
    /// Default status code for other methods (default: 404)
    /// </summary>
    public int DefaultStatusCode { get; set; } = StatusCodes.Status404NotFound;

    /// <summary>
    /// Include error message in response body (default: true)
    /// </summary>
    public bool IncludeMessage { get; set; } = true;

    /// <summary>
    /// Error code in response (default: "RESOURCE_NOT_FOUND")
    /// </summary>
    public string ErrorCode { get; set; } = "RESOURCE_NOT_FOUND";

    /// <summary>
    /// Error message in response (default: "The requested resource was not found.")
    /// </summary>
    public string ErrorMessage { get; set; } = "The requested resource was not found.";
}
```

## Usage Examples

### Example 1: Query Handler Returns Null

**Before (Without Middleware):**
```csharp
// Query Handler
public async Task<UserDto?> Handle(GetUserQuery query, CancellationToken ct)
{
    var user = await _repository.GetByIdAsync(query.UserId, ct);
    return user?.ToDto(); // Returns null if not found
}

// API Response
GET /api/users/{id}
Status: 200 OK
Body: null
```

**After (With Middleware):**
```csharp
// Query Handler (same code)
public async Task<UserDto?> Handle(GetUserQuery query, CancellationToken ct)
{
    var user = await _repository.GetByIdAsync(query.UserId, ct);
    return user?.ToDto(); // Returns null if not found
}

// API Response (middleware intercepts null)
GET /api/users/{id}
Status: 404 Not Found
Body: {
  "error": "RESOURCE_NOT_FOUND",
  "message": "The requested resource was not found.",
  "timestamp": "2025-12-14T10:30:00Z"
}
```

### Example 2: Delete Returns Null

```csharp
// Delete Handler
public async Task<bool?> Handle(DeleteUserCommand command, CancellationToken ct)
{
    var deleted = await _repository.DeleteAsync(command.UserId, ct);
    return deleted ? true : null; // Returns null if not found
}

// API Response (middleware intercepts null)
DELETE /api/users/{id}
Status: 204 No Content
Body: (empty)
```

### Example 3: Custom Configuration per API

```csharp
// In Auth.Api/Program.cs
app.UseNullResponseHandler(options =>
{
    options.ErrorCode = "USER_NOT_FOUND";
    options.ErrorMessage = "User account does not exist.";
});

// In Products.Api/Program.cs
app.UseNullResponseHandler(options =>
{
    options.ErrorCode = "PRODUCT_NOT_FOUND";
    options.ErrorMessage = "Product not found in catalog.";
    options.GetStatusCode = StatusCodes.Status404NotFound;
});
```

## Middleware Pipeline Order

**Recommended Order:**
```csharp
app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiting();
app.UseIdempotency();
app.UseRouting();
app.UseNullResponseHandler(); // ← Add here
app.UseAuthentication();
app.UseAuthorization();
app.UseJwtMiddleware();
app.MapAllEndpoints();
```

## When to Use

**✅ DO Use for:**
- Query handlers that return `Task<TDto?>` (nullable DTOs)
- APIs where null means "resource not found"
- Consistent error responses across all endpoints
- RESTful APIs following HTTP standards

**❌ DON'T Use for:**
- APIs that intentionally return null for business logic
- Non-JSON responses (HTML, XML, binary)
- Already handled null checks in handlers

## Benefits

- ✅ **Consistent API Responses**: All null responses handled uniformly
- ✅ **Proper HTTP Semantics**: Correct status codes for each scenario
- ✅ **Client-Friendly**: Clear error messages instead of null values
- ✅ **Zero Handler Changes**: Works with existing CQRS handlers
- ✅ **Configurable**: Customize per API or globally

## Integration with Existing Middleware

Works alongside other MagicTree.Framework.Middlewares packages:

| Middleware | Purpose | Order |
|------------|---------|-------|
| **RateLimit** | Request throttling | 1st (before routing) |
| **Idempotency** | Duplicate prevention | 2nd (before routing) |
| **NullResponse** | Null handling | 3rd (after routing) |
| **JwtMiddleware** | JWT parsing | 4th (after auth) |
| **AutoSaveChanges** | EF Core auto-save | Last (after endpoints) |

## Dependencies

- Microsoft.AspNetCore.App (framework reference)
- System.Text.Json (for JSON serialization)

## Troubleshooting

**Issue: Middleware not intercepting nulls**
- Ensure middleware is registered AFTER `UseRouting()` but BEFORE endpoints
- Check that response content-type is `application/json`

**Issue: Still getting 200 OK with null**
- Verify middleware is registered in the pipeline
- Check if another middleware is short-circuiting the response

**Issue: 204 No Content includes error message**
- By design, 204 responses have no body. Error message only for 404/other codes.

## Testing

```csharp
// Test GET returns 404 on null
var response = await _client.GetAsync("/api/users/00000000-0000-0000-0000-000000000000");
response.StatusCode.Should().Be(HttpStatusCode.NotFound);

// Test DELETE returns 204 on null
var deleteResponse = await _client.DeleteAsync("/api/users/00000000-0000-0000-0000-000000000000");
deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
```

## Version History

- **v1.0.0** (Dec 14, 2025): Initial implementation
  - Null detection for JSON responses
  - HTTP method-aware status codes
  - Configurable options
  - Standard error format
