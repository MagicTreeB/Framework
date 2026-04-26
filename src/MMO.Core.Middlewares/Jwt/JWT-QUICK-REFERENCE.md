# JWT Quick Reference Card

## 🚀 Quick Start

### Get Token from Auth API
```powershell
$response = Invoke-RestMethod -Uri "https://localhost:7001/connect/token" `
    -Method POST -ContentType "application/x-www-form-urlencoded" `
    -Body @{
        grant_type = "password"
        username = "admin@test.com"
        password = "Test@123456"
        scope = "openid email profile roles"
    } -SkipCertificateCheck

$token = $response.access_token
```

### Use Token in MMO API
```powershell
$headers = @{ "Authorization" = "Bearer $token" }
Invoke-RestMethod -Uri "https://localhost:7004/api/affiliates/me" -Headers $headers -SkipCertificateCheck
```

## 📝 Extension Methods

```csharp
using MMO.Core.Middlewares.Jwt;

// In endpoint
var jwtInfo = httpContext.GetJwtInfo();      // Get all info
var userId = httpContext.GetUserId();        // Get user ID only
var username = httpContext.GetUsername();    // Get username only
var email = httpContext.GetUserEmail();      // Get email only
var roles = httpContext.GetUserRoles();      // Get roles list
```

## 🔒 Protect Endpoints

```csharp
// Require authentication
group.MapPost<CreateRequest, ResponseDto>("/")
    .RequireAuthorization();

// Allow anonymous
group.MapGet<GetQuery, ResponseDto>("/{id}")
    .AllowAnonymous();

// Role-based
group.MapDelete<DeleteRequest, bool>("/{id}")
    .RequireAuthorization("Admin");

// In endpoint lambda
group.MapGet("/me", [Authorize] (HttpContext ctx) =>
{
    var jwtInfo = ctx.GetJwtInfo();
    return Results.Ok(jwtInfo);
});
```

## 🎯 Use in Handlers

```csharp
public class YourHandler : IHandler<YourRequest, YourResponse>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public YourHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async Task<IResult<YourResponse>> Handle(YourRequest request, CancellationToken ct)
    {
        // Get user info
        var userId = _httpContextAccessor.HttpContext?.GetUserId();
        var roles = _httpContextAccessor.HttpContext?.GetUserRoles();
        
        // Check authentication
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Fail<YourResponse>(new Error("Unauthorized"));
        }
        
        // Check role
        if (!roles.Contains("Admin"))
        {
            return Result.Fail<YourResponse>(new Error("Forbidden"));
        }
        
        // Your logic here
        return Result.Ok(new YourResponse());
    }
}
```

## ⚙️ Configuration

### Program.cs
```csharp
using MMO.Core.Middlewares.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;

// Services
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.Authority = "https://localhost:7001";
        options.Audience = "your_api";
        options.RequireHttpsMetadata = false;
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// Pipeline
app.UseAuthentication();
app.UseJwtMiddleware();
app.UseAuthorization();
```

### appsettings.json
```json
{
  "AuthApi": {
    "Url": "https://localhost:7001"
  }
}
```

## 🧪 Test Script

```powershell
cd D:\Bao\MyProject\MMO\Apis\Mmo
.\test-jwt-integration.ps1
```

## 📊 JWT Structure

```json
{
  "sub": "user-guid",           // User ID
  "name": "John Doe",           // Username
  "email": "john@example.com",  // Email
  "role": ["Admin", "User"],    // Roles
  "iss": "https://localhost:7001",
  "aud": ["mmo_api", "openid"],
  "exp": 1734567890
}
```

## 💡 Common Patterns

### Check Authentication
```csharp
var jwtInfo = httpContext.GetJwtInfo();
if (jwtInfo == null) return Results.Unauthorized();
```

### Check Role
```csharp
var roles = httpContext.GetUserRoles();
if (!roles.Contains("Admin")) return Results.Forbid();
```

### Check Ownership
```csharp
var userId = httpContext.GetUserId();
if (entity.CreatedBy != userId) return Results.Forbid();
```

### Optional Auth
```csharp
var userId = httpContext.GetUserId();
if (userId != null) {
    // Logged in user
} else {
    // Anonymous user
}
```

## 🔧 Troubleshooting

### 401 Unauthorized
- Check token in Authorization header: `Bearer {token}`
- Verify token not expired (exp claim)
- Ensure Auth API is running

### 403 Forbidden
- Check user has required role
- Verify ownership of resource

### Token Not Validated
- Check `AuthApi.Url` in appsettings.json
- Verify Auth API is accessible
- Check `options.Audience` matches token audience

## 📚 Documentation

- **Full Guide**: `JWT-INTEGRATION.md`
- **Handler Guide**: `JWT-HANDLER-GUIDE.md`
- **Summary**: `JWT-INTEGRATION-SUMMARY.md`

## ✅ Checklist for New API

- [ ] Add JWT packages to Program.cs
- [ ] Configure authentication/authorization
- [ ] Add middleware to pipeline
- [ ] Add `AuthApi.Url` to appsettings.json
- [ ] Register `IHttpContextAccessor`
- [ ] Protect endpoints with `.RequireAuthorization()`
- [ ] Use `httpContext.GetJwtInfo()` in handlers
- [ ] Test with token from Auth API

## 🎉 You're Ready!

Run the test script and start protecting your endpoints!
