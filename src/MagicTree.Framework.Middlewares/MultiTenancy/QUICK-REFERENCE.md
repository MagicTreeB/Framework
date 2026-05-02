# Multi-Tenancy Quick Reference

## 🎯 What You Get

✅ **Host role**: See ALL data from ALL organizations (no filtering)
✅ **Admin/Staff/Client**: See ONLY data from their organization (automatic filtering)
✅ **Zero boilerplate**: No manual `where x.OrganizationId == orgId` in queries
✅ **Secure by default**: Impossible to accidentally see other tenants' data

---

## 🚀 Quick Setup (5 Steps)

### 1️⃣ Update Auth.Api - Add OrganizationId to User

```csharp
// Auth.Domain/Entities/ApplicationUser.cs
public class ApplicationUser : IdentityUser<Guid>
{
    public Guid OrganizationId { get; set; } // ADD THIS
}
```

```bash
cd Apis/Auth
.\add-migration.bat AddOrganizationIdToUser
.\update-database.bat
```

### 2️⃣ Update Auth.Api - Add OrganizationId to JWT Claims

```csharp
// Auth.Application/Features/Users/Actions/LoginRequest.cs - LoginHandler
var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Name, user.UserName!),
    new Claim(ClaimTypes.Email, user.Email!),
    new Claim("OrganizationId", user.OrganizationId.ToString()), // ADD THIS
};
```

### 3️⃣ Register Middleware in Your API

```csharp
// YourApi.Api/Program.cs
using MagicTree.Framework.Middlewares.MultiTenancy;

var builder = WebApplication.CreateBuilder(args);

// REQUIRED: Register HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Register DbContext
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
});

var app = builder.Build();

// CRITICAL ORDER:
app.UseJwtMiddleware();         // 1️⃣ Extract JWT
app.UseTenantQueryFilter();     // 2️⃣ Extract OrganizationId (ADD THIS)
app.UseAuthentication();        // 3️⃣ Authenticate
app.UseAuthorization();         // 4️⃣ Authorize
```

### 4️⃣ Update Your DbContext

```csharp
// YourApi.Infra/Data/ApplicationDbContext.cs
using MagicTree.Framework.Middlewares.MultiTenancy;
using System.Linq.Expressions;

public class ApplicationDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    // ADD constructor parameter
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        ApplyTenantQueryFilters(modelBuilder); // ADD THIS
    }
    
    // ADD this method (copy from ExampleDbContext.cs)
    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        // See ExampleDbContext.cs for full implementation
    }
}
```

### 5️⃣ Create Tenant-Isolated Entities

```csharp
// YourApi.Domain/Entities/Product.cs
using MagicTree.Framework.Entity.Entities;

public class Product : OwnerOrganizationEntity // Inherit from this
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    
    public static Product Create(string name, decimal price, Guid organizationId)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            OrganizationId = organizationId, // IMPORTANT!
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow,
        };
    }
}
```

---

## 💡 Usage in Handlers

### Query Handler (Automatic Filtering)
```csharp
public class GetAllProductsHandler : IHandler<GetAllProductsQuery, List<ProductDto>>
{
    private readonly IRepository<Product> _repository;
    
    public async Task<List<ProductDto>> Handle(GetAllProductsQuery query, CancellationToken ct)
    {
        // ✅ Host: Returns ALL products
        // ✅ Admin/Staff/Client: Returns ONLY products from their OrganizationId
        // ✅ NO manual filtering needed!
        var products = await _repository.GetAllAsync(ct);
        return products.Select(p => p.Adapt<ProductDto>()).ToList();
    }
}
```

### Command Handler (Get OrganizationId)
```csharp
using MagicTree.Framework.Middlewares.MultiTenancy;

public class CreateProductHandler : IHandler<CreateProductRequest, ProductDto>
{
    private readonly IRepository<Product> _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public async Task<IResult<ProductDto>> Handle(CreateProductRequest request, CancellationToken ct)
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        
        // Get OrganizationId from tenant context
        var organizationId = httpContext.GetRequiredOrganizationId(); // ✅ Extension method
        
        var product = Product.Create(request.Name, request.Price, organizationId);
        await _repository.AddAsync(product, ct);
        
        return Result.Ok(product.Adapt<ProductDto>());
    }
}
```

### Host Override (Bypass Filtering)
```csharp
public class GetAllProductsAdminHandler : IHandler<GetAllProductsAdminQuery, List<ProductDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public async Task<List<ProductDto>> Handle(GetAllProductsAdminQuery query, CancellationToken ct)
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        
        // Only Host can see all products
        if (!httpContext.IsHostUser()) // ✅ Extension method
        {
            throw new UnauthorizedAccessException("Only Host can view all products");
        }
        
        // Bypass automatic filtering
        var products = await _context.Products
            .IgnoreQueryFilters() // ✅ EF Core method
            .ToListAsync(ct);
        
        return products.Select(p => p.Adapt<ProductDto>()).ToList();
    }
}
```

---

## 🛠️ Helper Extension Methods

```csharp
using MagicTree.Framework.Middlewares.MultiTenancy;

// In any handler with IHttpContextAccessor
var httpContext = _httpContextAccessor.HttpContext!;

// Get tenant context
var tenantContext = httpContext.GetTenantContext();

// Get OrganizationId (nullable)
Guid? orgId = httpContext.GetOrganizationId();

// Get OrganizationId (throws if not found)
Guid requiredOrgId = httpContext.GetRequiredOrganizationId();

// Check if Host user
bool isHost = httpContext.IsHostUser();
```

---

## 📋 Checklist for Each API

- [ ] Add middleware: `app.UseTenantQueryFilter()`
- [ ] Register `IHttpContextAccessor` in DI
- [ ] Update DbContext constructor
- [ ] Add `ApplyTenantQueryFilters()` method
- [ ] Inherit entities from `OwnerOrganizationEntity`
- [ ] Use `GetRequiredOrganizationId()` in command handlers
- [ ] Generate and apply migrations
- [ ] Test with Admin and Host roles

---

## 🧪 Testing

```powershell
# Test as Admin (sees only their organization's data)
$adminToken = "eyJhbGc..." # OrganizationId = "11111111-1111-1111-1111-111111111111"
Invoke-WebRequest -Uri "https://localhost:7001/api/products" `
  -Headers @{"Authorization" = "Bearer $adminToken"}

# Test as Host (sees ALL organizations' data)
$hostToken = "eyJhbGc..." # Role = "Host"
Invoke-WebRequest -Uri "https://localhost:7001/api/products" `
  -Headers @{"Authorization" = "Bearer $hostToken"}
```

---

## ⚠️ Important Notes

1. **Middleware Order**: `UseJwtMiddleware()` → `UseTenantQueryFilter()` → `UseAuthentication()`
2. **HttpContextAccessor**: MUST be registered before DbContext
3. **Factory Methods**: Always pass `OrganizationId` when creating entities
4. **Host Override**: Use `IgnoreQueryFilters()` when Host needs to see all data
5. **Auth.Api Only**: Only Auth.Api needs to add `OrganizationId` to JWT claims

---

## 📚 Full Documentation

See `MagicTree.Framework.Middlewares/MultiTenancy/README.md` for complete guide.
