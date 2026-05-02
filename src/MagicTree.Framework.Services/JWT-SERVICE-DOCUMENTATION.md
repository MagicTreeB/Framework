# MagicTree.Framework.Services - JWT Service Documentation

**Date**: December 21, 2025  
**Package**: MagicTree.Framework.Services  
**Purpose**: Simplified JWT service for accessing current user information from HttpContext

## Overview

The JWT Service provides a clean, injectable service for accessing JWT information in handlers and services without directly depending on `IHttpContextAccessor`.

## Architecture

### Components

1. **IJwtService** (`Jwt/IJwtService.cs`) - Interface with 4 methods
2. **JwtService** (`Jwt/JwtService.cs`) - Implementation using IHttpContextAccessor
3. **JwtServiceExtensions** (`Extensions/JwtServiceExtensions.cs`) - DI registration

### Design Principles

- **Thin Wrapper**: Delegates to existing JWT middleware infrastructure
- **No Token Generation**: This service only READS JWT info, doesn't create tokens
- **Middleware Dependency**: Requires `MagicTree.Framework.Middlewares` with JWT middleware

## Dependencies

### Package References
```xml
<PackageReference Include="Microsoft.AspNetCore.Http.Abstractions" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
```

### Project References
```xml
<ProjectReference Include="..\MagicTree.Framework.Middlewares\MagicTree.Framework.Middlewares.csproj" />
```

## API Reference

### IJwtService Methods

```csharp
// Get complete JWT information
JwtInfo? GetJwtInfo();

// Get current user ID
string? GetUserId();

// Get current username
string? GetUserName();

// Get organization ID from JWT claims
Guid? GetOrganizationId();
```

## Usage

### Registration (Program.cs)

```csharp
using MagicTree.Framework.Services.Extensions;

// Register JWT service
builder.Services.AddJwtService();
```

### In CQRS Handlers

```csharp
public class CreateBrandHandler : IHandler<CreateBrandRequest, BrandDto>
{
    private readonly IRepository<Brand> _repository;
    private readonly IJwtService _jwtService;
    
    public CreateBrandHandler(
        IRepository<Brand> repository,
        IJwtService jwtService)
    {
        _repository = repository;
        _jwtService = jwtService;
    }
    
    public async Task<IResult<BrandDto>> OnHandle(
        CreateBrandRequest request, 
        CancellationToken ct)
    {
        // Get user info from JWT
        var userId = _jwtService.GetUserId() ?? "System";
        var organizationId = _jwtService.GetOrganizationId() ?? Guid.Empty;
        
        // Use in entity creation
        var brand = Brand.Create(
            request.Name,
            request.Description,
            request.IsActive,
            organizationId,
            userId
        );
        
        await _repository.AddAsync(brand, ct);
        return Result.Ok(brand.Adapt<BrandDto>());
    }
}
```

### In Services

```csharp
public class BrandService
{
    private readonly IJwtService _jwtService;
    
    public BrandService(IJwtService jwtService)
    {
        _jwtService = jwtService;
    }
    
    public async Task<Result> DoSomething()
    {
        var jwtInfo = _jwtService.GetJwtInfo();
        if (jwtInfo == null)
            return Result.Fail("Not authenticated");
        
        var userId = jwtInfo.UserId;
        var roles = jwtInfo.Roles;
        var permissions = jwtInfo.Permissions;
        
        // Business logic...
    }
}
```

## Implementation Details

### GetOrganizationId()

The `GetOrganizationId()` method extracts the organization ID from the `organization_id` claim in the JWT token:

```csharp
public Guid? GetOrganizationId()
{
    var context = _httpContextAccessor.HttpContext;
    if (context == null) return null;

    var jwtInfo = context.GetJwtInfo();
    if (jwtInfo?.Claims.TryGetValue("organization_id", out var orgIdString) == true)
    {
        if (Guid.TryParse(orgIdString, out var orgId))
        {
            return orgId;
        }
    }

    return null;
}
```

**Note**: Auth.Api must include `organization_id` claim when generating JWT tokens.

## Benefits

1. ✅ **Cleaner Code**: No IHttpContextAccessor in every handler
2. ✅ **Testable**: Easy to mock IJwtService in unit tests
3. ✅ **Consistent**: Single source of truth for JWT access
4. ✅ **Type-Safe**: Guid parsing for OrganizationId
5. ✅ **Null-Safe**: Returns null when not authenticated

## Middleware Dependency

**IMPORTANT**: This service requires JWT middleware to be registered:

```csharp
// In Program.cs (middleware order matters)
app.UseAuthentication();
app.UseJwtMiddleware();        // Required!
app.UseAuthorization();
```

The JWT middleware populates `HttpContext.Items["JwtInfo"]` which this service reads.

## Testing

### Unit Test Example

```csharp
public class CreateBrandHandlerTests
{
    [Fact]
    public async Task Handle_UsesJwtServiceForUserId()
    {
        // Arrange
        var mockJwtService = new Mock<IJwtService>();
        mockJwtService.Setup(x => x.GetUserId())
            .Returns("test-user-123");
        mockJwtService.Setup(x => x.GetOrganizationId())
            .Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        
        var handler = new CreateBrandHandler(
            mockRepository.Object,
            mockJwtService.Object
        );
        
        // Act
        var result = await handler.OnHandle(request, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        mockJwtService.Verify(x => x.GetUserId(), Times.Once);
    }
}
```

## Migration from HttpContext Extensions

### Before (Direct HttpContext Access)

```csharp
public class CreateBrandHandler
{
    public async Task<IResult<BrandDto>> OnHandle(
        CreateBrandRequest request,
        CancellationToken ct)
    {
        // ❌ Direct HttpContext access
        var userId = httpContext.GetUserId() ?? "System";
        var organizationId = httpContext.GetOrganizationId() ?? Guid.Empty;
    }
}
```

### After (IJwtService)

```csharp
public class CreateBrandHandler
{
    private readonly IJwtService _jwtService;
    
    public async Task<IResult<BrandDto>> OnHandle(
        CreateBrandRequest request,
        CancellationToken ct)
    {
        // ✅ Clean service injection
        var userId = _jwtService.GetUserId() ?? "System";
        var organizationId = _jwtService.GetOrganizationId() ?? Guid.Empty;
    }
}
```

## Namespace Structure

```
MagicTree.Framework.Services
├── Jwt/
│   ├── IJwtService.cs           // Interface
│   └── JwtService.cs            // Implementation
└── Extensions/
    └── JwtServiceExtensions.cs  // DI registration
```

## Common Patterns

### Pattern 1: Required Authentication

```csharp
var userId = _jwtService.GetUserId();
if (string.IsNullOrEmpty(userId))
    return Result.Fail("Authentication required");
```

### Pattern 2: Optional User Info

```csharp
var userId = _jwtService.GetUserId() ?? "System";
var organizationId = _jwtService.GetOrganizationId() ?? Guid.Empty;
```

### Pattern 3: Full JWT Info

```csharp
var jwtInfo = _jwtService.GetJwtInfo();
if (jwtInfo == null)
    return Results.Unauthorized();

var hasPermission = jwtInfo.Permissions.Contains("brand.create");
```

## Future Enhancements

Potential additions (not implemented yet):

- [ ] `GetUserRoles()` - Get user roles list
- [ ] `GetUserPermissions()` - Get user permissions list
- [ ] `GetUserEmail()` - Get user email
- [ ] `GetTenantId()` - Alias for GetOrganizationId()
- [ ] `IsAuthenticated()` - Check if user is authenticated
- [ ] `HasRole(string role)` - Check specific role
- [ ] `HasPermission(string permission)` - Check specific permission

## Related Documentation

- `Core/MagicTree.Framework.Middlewares/Jwt/JWT-QUICK-REFERENCE.md` - JWT Middleware
- `Core/MagicTree.Framework.Middlewares/MultiTenancy/README.md` - Multi-tenancy with OrganizationId
- `.github/copilot-instructions.md` - Architecture patterns

## Build Information

- **Last Updated**: December 21, 2025
- **Build Status**: ✅ Successful
- **Package Version**: Follows Directory.Packages.props
- **Target Framework**: net10.0
