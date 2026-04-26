# Multi-Tenancy Implementation Guide

## Overview

This multi-tenancy solution provides **automatic tenant isolation** using `OrganizationId`:
- **Host role**: See all data across all organizations (no filter)
- **Admin/Staff/Client roles**: Auto-filter by their `OrganizationId` (tenant isolation)

## Architecture Components

### 1. Core Components (MMO.Core.Middlewares)

- **`TenantContext`** - Stores current user's tenant information
- **`TenantQueryFilterMiddleware`** - Extracts tenant info from JWT and stores in HttpContext
- **`TenantQueryFilterExtensions`** - Extension method for middleware registration
- **`TenantContextAccessor`** - Helper methods for accessing tenant info

### 2. Entity Base Class (MMO.Core.Entity)

- **`OwnerOrganizationEntity`** - Base class for tenant-isolated entities

## Implementation Steps

### Step 1: Update ApplicationUser (Auth.Api)

Add `OrganizationId` property to ApplicationUser:

```csharp
// Auth.Domain/Entities/ApplicationUser.cs
public class ApplicationUser : IdentityUser<Guid>
{
    // Existing properties...
    
    /// <summary>
    /// Organization/Tenant identifier for multi-tenancy
    /// </summary>
    public Guid OrganizationId { get; set; }
    
    // Other properties...
}
```

**Migration Required:**
```bash
cd Apis/Auth
.\add-migration.bat AddOrganizationIdToApplicationUser
.\update-database.bat
```

### Step 2: Update JWT Claims (Auth.Api)

Add `OrganizationId` to JWT token:

```csharp
// Auth.Application/Features/Users/Actions/LoginRequest.cs
public class LoginHandler : IHandler<LoginRequest, LoginResponse>
{
    public async Task<IResult<LoginResponse>> Handle(LoginRequest request, CancellationToken ct)
    {
        // ... existing authentication logic ...
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim("OrganizationId", user.OrganizationId.ToString()), // ADD THIS
        };
        
        // Add role claims
        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        
        // ... token generation ...
    }
}
```

### Step 3: Register Middleware (All APIs)

Update Program.cs in each API:

```csharp
// YourApi.Api/Program.cs
using MMO.Core.Middlewares.MultiTenancy;

var builder = WebApplication.CreateBuilder(args);

// Register HttpContextAccessor (REQUIRED for tenant filtering)
builder.Services.AddHttpContextAccessor();

// Register DbContext
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
});

var app = builder.Build();

// Middleware order is CRITICAL:
app.UseJwtMiddleware();         // 1. Extract JWT claims
app.UseTenantQueryFilter();     // 2. Extract tenant context
app.UseAuthentication();        // 3. Authenticate user
app.UseAuthorization();         // 4. Authorize user

app.MapAllEndpoints("YourApi.ApiEndpoints");
```

### Step 4: Configure DbContext with Global Query Filters

Update your DbContext to apply automatic tenant filtering:

```csharp
// YourApi.Infra/Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using MMO.Core.Entity.Entities;
using MMO.Core.Middlewares.MultiTenancy;
using System.Linq.Expressions;

namespace YourApi.Infra.Data;

public class ApplicationDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        // Apply global query filters for multi-tenancy
        ApplyTenantQueryFilters(modelBuilder);
    }
    
    /// <summary>
    /// Applies automatic OrganizationId filtering to all entities
    /// inheriting from OwnerOrganizationEntity.
    /// Host role users bypass filtering.
    /// </summary>
    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        if (httpContext == null)
            return;
        
        // Get tenant context from middleware
        var tenantContext = httpContext.GetTenantContext();
        
        if (tenantContext == null)
            return;
        
        // Apply filter to all entities inheriting OwnerOrganizationEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Check if entity inherits from OwnerOrganizationEntity
            if (typeof(OwnerOrganizationEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Host role: no filter (see all data)
                if (tenantContext.IsHost)
                    continue;
                
                // Other roles: filter by OrganizationId
                if (tenantContext.OrganizationId.HasValue)
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, nameof(OwnerOrganizationEntity.OrganizationId));
                    var filterValue = Expression.Constant(tenantContext.OrganizationId.Value);
                    var filter = Expression.Lambda(
                        Expression.Equal(property, filterValue),
                        parameter
                    );
                    
                    entityType.SetQueryFilter(filter);
                }
            }
        }
    }
}
```

### Step 5: Create Tenant-Isolated Entities

Inherit from `OwnerOrganizationEntity`:

```csharp
// YourApi.Domain/Entities/Product.cs
using MMO.Core.Entity.Entities;

namespace YourApi.Domain.Entities;

/// <summary>
/// Product entity with automatic tenant isolation.
/// Queries will be automatically filtered by OrganizationId
/// except for Host role users.
/// </summary>
public class Product : OwnerOrganizationEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Factory method to create a new Product
    /// </summary>
    public static Product Create(
        string name, 
        string description, 
        decimal price, 
        Guid organizationId,
        string? createdBy = null)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price,
            OrganizationId = organizationId,
            IsActive = true,
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow,
            CreatedBy = createdBy ?? "System",
            UpdatedBy = createdBy ?? "System"
        };
    }
    
    public void Update(string name, string description, decimal price, string? updatedBy = null)
    {
        Name = name;
        Description = description;
        Price = price;
        UpdatedOn = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy ?? "System";
    }
}
```

### Step 6: Use in Handlers

**Query Handler (Automatic Filtering):**
```csharp
// YourApi.Application/Features/Products/Queries/GetAllProductsQuery.cs
using YourApi.Domain.Entities;
using YourApi.Domain.Repositories;
using Fluents.Queries;

namespace YourApi.Application.Features.Products.Queries;

public class GetAllProductsQuery : IWitResponse<List<ProductDto>>
{
}

public class GetAllProductsQueryValidator : AbstractValidator<GetAllProductsQuery>
{
    public GetAllProductsQueryValidator()
    {
        // No validation needed
    }
}

public class GetAllProductsHandler : IHandler<GetAllProductsQuery, List<ProductDto>>
{
    private readonly IRepository<Product> _repository;
    
    public GetAllProductsHandler(IRepository<Product> repository)
    {
        _repository = repository;
    }
    
    public async Task<List<ProductDto>> Handle(GetAllProductsQuery query, CancellationToken ct)
    {
        // ✅ Host role: Returns ALL products across all organizations
        // ✅ Admin/Staff/Client: Returns ONLY products from their OrganizationId
        // NO manual filtering needed!
        var products = await _repository.GetAllAsync(ct);
        return products.Select(p => p.Adapt<ProductDto>()).ToList();
    }
}
```

**Command Handler (Manual OrganizationId Assignment):**
```csharp
// YourApi.Application/Features/Products/Actions/CreateProductRequest.cs
using Microsoft.AspNetCore.Http;
using MMO.Core.Middlewares.MultiTenancy;
using YourApi.Domain.Entities;
using YourApi.Domain.Repositories;
using Fluents.Requests;
using FluentResults;
using FluentValidation;

namespace YourApi.Application.Features.Products.Actions;

public class CreateProductRequest : IWitResponse<ProductDto>
{
    [Description("Product name")]
    public required string Name { get; init; }
    
    [Description("Product description")]
    public required string Description { get; init; }
    
    [Description("Product price")]
    public required decimal Price { get; init; }
}

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(200);
        
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(1000);
        
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0");
    }
}

public class CreateProductHandler : IHandler<CreateProductRequest, ProductDto>
{
    private readonly IRepository<Product> _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public CreateProductHandler(
        IRepository<Product> repository,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async Task<IResult<ProductDto>> Handle(CreateProductRequest request, CancellationToken ct)
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        
        // Get OrganizationId from tenant context
        var organizationId = httpContext.GetRequiredOrganizationId();
        var jwtInfo = httpContext.GetJwtInfo()!;
        
        // Create product with user's OrganizationId
        var product = Product.Create(
            request.Name,
            request.Description,
            request.Price,
            organizationId,
            jwtInfo.UserId
        );
        
        await _repository.AddAsync(product, ct);
        // AutoSaveChanges middleware handles SaveChangesAsync
        
        return Result.Ok(product.Adapt<ProductDto>());
    }
}
```

**Host Override (Bypass Filtering):**
```csharp
// YourApi.Application/Features/Products/Queries/GetAllProductsAdminQuery.cs
public class GetAllProductsAdminHandler : IHandler<GetAllProductsAdminQuery, List<ProductDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public GetAllProductsAdminHandler(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async Task<List<ProductDto>> Handle(GetAllProductsAdminQuery query, CancellationToken ct)
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        
        // Check if user is Host (can see all organizations)
        if (!httpContext.IsHostUser())
        {
            throw new UnauthorizedAccessException("Only Host can view all products");
        }
        
        // Use IgnoreQueryFilters to bypass tenant filtering
        var products = await _context.Products
            .IgnoreQueryFilters() // <-- Bypass automatic filtering
            .Where(p => p.IsActive)
            .ToListAsync(ct);
        
        return products.Select(p => p.Adapt<ProductDto>()).ToList();
    }
}
```

## Usage Examples

### Example 1: Helper Methods
```csharp
// In any handler with IHttpContextAccessor
var httpContext = _httpContextAccessor.HttpContext!;

// Get tenant context
var tenantContext = httpContext.GetTenantContext();

// Get OrganizationId (nullable)
var orgId = httpContext.GetOrganizationId();

// Get OrganizationId (throws if not found)
var requiredOrgId = httpContext.GetRequiredOrganizationId();

// Check if Host user
var isHost = httpContext.IsHostUser();
```

### Example 2: Seed Data with OrganizationId
```csharp
// YourApi.Infra/Data/YourDbContextSeed.cs
public static async Task SeedDataAsync(ApplicationDbContext context)
{
    // Create organizations
    var org1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    var org2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    
    // Seed products for organization 1
    if (!context.Products.Any(p => p.OrganizationId == org1))
    {
        context.Products.Add(Product.Create("Product A", "Description A", 99.99m, org1));
        context.Products.Add(Product.Create("Product B", "Description B", 149.99m, org1));
    }
    
    // Seed products for organization 2
    if (!context.Products.Any(p => p.OrganizationId == org2))
    {
        context.Products.Add(Product.Create("Product C", "Description C", 199.99m, org2));
        context.Products.Add(Product.Create("Product D", "Description D", 249.99m, org2));
    }
    
    await context.SaveChangesAsync();
}
```

## Testing

### Test as Admin (Filtered)
```powershell
# Admin user only sees products from their OrganizationId
$adminToken = "eyJhbGc..." # Token with OrganizationId claim

Invoke-WebRequest -Uri "https://localhost:7001/api/products" `
  -Method GET `
  -Headers @{"Authorization" = "Bearer $adminToken"}
```

### Test as Host (Unfiltered)
```powershell
# Host user sees ALL products from ALL organizations
$hostToken = "eyJhbGc..." # Token with Host role

Invoke-WebRequest -Uri "https://localhost:7001/api/products" `
  -Method GET `
  -Headers @{"Authorization" = "Bearer $hostToken"}
```

## Migration Checklist

For each API that needs multi-tenancy:

- [ ] Add `OrganizationId` to ApplicationUser (Auth.Api only)
- [ ] Update JWT claims to include `OrganizationId` (Auth.Api only)
- [ ] Register `IHttpContextAccessor` in Program.cs
- [ ] Add `app.UseTenantQueryFilter()` middleware
- [ ] Update DbContext constructor to accept `IHttpContextAccessor`
- [ ] Add `ApplyTenantQueryFilters()` method to DbContext
- [ ] Create entities inheriting from `OwnerOrganizationEntity`
- [ ] Add EF Core entity configurations
- [ ] Generate and apply migrations
- [ ] Update command handlers to use `GetRequiredOrganizationId()`
- [ ] Test with Admin and Host roles

## Benefits

✅ **Automatic Tenant Isolation**: No manual filtering needed in 95% of queries
✅ **Host Bypass**: Host role sees all data without code changes
✅ **Type-Safe**: Compile-time checking of OrganizationId property
✅ **Clean Codebase**: No repetitive filtering logic
✅ **Secure by Default**: Impossible to accidentally query other tenants' data
✅ **EF Core Integration**: Works with all LINQ queries, includes, and joins
✅ **Performance**: Filter applied at SQL level, not in-memory

## Important Notes

1. **Middleware Order**: Must be after `UseJwtMiddleware` to access user claims
2. **HttpContextAccessor**: Required for DbContext to access current user's tenant
3. **IgnoreQueryFilters()**: Use sparingly when Host needs to bypass filtering
4. **Factory Methods**: Always pass `OrganizationId` when creating entities
5. **Testing**: Test both Admin (filtered) and Host (unfiltered) scenarios
6. **Migration**: Add `OrganizationId` column to existing tables
7. **Seed Data**: Assign `OrganizationId` to all existing data

## Troubleshooting

### Issue: "OrganizationId not found in token"
**Solution**: Ensure `OrganizationId` claim is added to JWT in LoginHandler

### Issue: Query returns empty even though data exists
**Solution**: Check that `app.UseTenantQueryFilter()` is registered AFTER `app.UseJwtMiddleware()`

### Issue: Host user still sees filtered data
**Solution**: Verify that `IsHost` check is working in `ApplyTenantQueryFilters()`

### Issue: Cannot create entity - OrganizationId not set
**Solution**: Use factory methods that accept `OrganizationId` parameter, get it from `httpContext.GetRequiredOrganizationId()`
