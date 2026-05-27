# MMO.Core Framework - Quick Reference Guide

## 📦 Project Structure at a Glance

```
33 Projects Total
├── 24 Core & Service Projects
│   ├── Layer 1: Domain (Entity, Share, Dtos, Contracts, Exceptions)
│   ├── Layer 2: Services (Services, Extensions, CodeGenerator, Configs)
│   ├── Layer 3: Infrastructure (BlobStorage, RabbitMQ, Hangfire, Email, VideoStorage, SignalR)
│   └── Layer 4: Cross-Cutting (Middlewares, RateLimit, Idempotency, HybridCache, Metrics)
└── 9 Unit Test Projects (*.UnitTest suffix)
```

---

## 🏗️ Architectural Patterns

| Pattern | Purpose | Key Package |
|---------|---------|-------------|
| **Clean Architecture** | Layered separation | All core packages |
| **DDD (Domain-Driven Design)** | Rich domain models | MMO.Core.Entity |
| **CQRS** | Separate read/write | MMO.Core.Extensions |
| **Repository Pattern** | Data access abstraction | Auto-discovered `I*Repository` |
| **Service-Oriented** | Composition over inheritance | MMO.Core.Services |
| **Multi-Tenancy** | Data isolation | `OwnerOrganizationEntity<T>` |
| **Global Exception Handling** | Centralized error handling | MMO.Core.Exceptions |

---

## 📝 Naming Conventions Cheat Sheet

### Classes

```
Entities:        [Domain]Entity, Base[Domain]Entity
DTOs:            [Domain]Dto, [Domain]ResponseDto, [Domain]InfoDto
Services:        I[Service]Service, [Service]Service
Handlers:        [Command/Query]Handler
Repositories:    I[Entity]Repository, [Entity]Repository
Events:          [Action][Entity]Event
Exceptions:      [Specific]Exception
Extensions:      [Domain]Extension(s)
```

### Namespaces

```
MMO.Core.[Feature]
  └─ MMO.Core.Entity
  └─ MMO.Core.Services
  └─ MMO.Core.Middlewares
  └─ MMO.Core.RabbitMQ
  etc.
```

---

## 🔄 CQRS Implementation Quick Start

### 1. Create Command/Query

```csharp
public class CreateBrandCommand : IWitResponse<BrandDto>
{
    public string Name { get; set; }
}
```

### 2. Create Handler

```csharp
public class CreateBrandHandler : IHandler<CreateBrandCommand, BrandDto>
{
    public async Task<BrandDto> Handle(CreateBrandCommand command, CancellationToken ct)
    {
        // Business logic
        var brand = new Brand(command.Name);
        await _repository.AddAsync(brand, ct);
        return brand.ToDto();
    }
}
```

### 3. Register in Program.cs

```csharp
builder.Services.AddCqrsHandlers(typeof(Program).Assembly);
```

### 4. Map Endpoint

```csharp
group.MapCqrsPost<CreateBrandCommand, BrandDto>("")
```

---

## 🗄️ Entity Pattern Quick Start

### 1. Single-Tenant Entity

```csharp
public class Brand : BaseEntity<Guid>
{
    public string Name { get; private set; }
    
    public Brand(string name, string userId) 
        : base(Guid.NewGuid(), userId, "System") { }
}
```

**Inherited**:
- ✅ `Id`, `CreatedOn`, `CreatedBy`, `UpdatedOn`, `UpdatedBy`
- ✅ `IsDeleted` (soft delete)
- ✅ `SetCreated()`, `SetUpdated()`, `SoftDelete()`, `Restore()`

### 2. Multi-Tenant Entity

```csharp
public class BrandSettings : OwnerOrganizationEntity<Guid>
{
    public string? Theme { get; set; }
    
    public BrandSettings(Guid organizationId, string userId)
        : base(Guid.NewGuid(), organizationId, userId) { }
}
```

**Inherited**: Everything from BaseEntity + automatic `OrganizationId` filtering

---

## 📚 Repository Pattern Quick Start

### 1. Define Interface

```csharp
public interface IBrandRepository
{
    Task<Brand?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<Brand>> ListAllAsync(CancellationToken ct);
    Task AddAsync(Brand entity, CancellationToken ct);
}
```

### 2. Implement

```csharp
public class BrandRepository : IBrandRepository
{
    // Implementation
}
```

### 3. Auto-Register

```csharp
builder.Services.AddRepositories(typeof(Program).Assembly);
```

**Convention**: Any `I*Repository` is automatically registered as **scoped**.

---

## 🚀 API Endpoint Quick Start

```csharp
var brandGroup = app.MapGroup("/api/brands")
    .WithName("Brands")
    .WithOpenApi();

// CREATE
brandGroup.MapCqrsPost<CreateBrandCommand, BrandDto>("")
    .WithName("Create Brand");

// READ (single)
brandGroup.MapCqrsGet<GetBrandQuery, BrandDto>("{id}")
    .WithName("Get Brand");

// READ (list)
brandGroup.MapCqrsGet<ListBrandsQuery, List<BrandDto>>("")
    .WithName("List Brands");

// UPDATE
brandGroup.MapCqrsPut<UpdateBrandCommand, BrandDto>("{id}")
    .WithName("Update Brand");

// DELETE
brandGroup.MapCqrsDelete<DeleteBrandCommand, bool>("{id}")
    .WithName("Delete Brand");
```

---

## ⚠️ Exception Handling Quick Start

### 1. Throw Domain Exception

```csharp
if (brand == null)
    throw new EntityNotFoundException<Guid>("Brand", id);

if (existingBrand != null)
    throw new EntityAlreadyExistsException("Brand", "Name", name);

if (!isValid)
    throw new EntityValidationException("Brand", "InvalidData");
```

### 2. Auto Mapping (No Handler Needed)

```csharp
// EntityNotFoundException<Guid> → HTTP 404
// EntityAlreadyExistsException → HTTP 409
// EntityValidationException → HTTP 400
// InvalidEntityOperationException → HTTP 400
// UnauthorizedEntityAccessException → HTTP 403
```

### 3. Global Registration

```csharp
app.UseGlobalExceptionHandler();  // Place before UseAuthentication
```

---

## 🔐 JWT Service Quick Start

```csharp
public class GetUserHandler : IHandler<GetUserQuery, UserDto?>
{
    private readonly IJwtService _jwtService;
    
    public async Task<UserDto?> Handle(GetUserQuery query, CancellationToken ct)
    {
        var userId = _jwtService.GetUserId();  // Current user ID
        var userName = _jwtService.GetUserName();  // Current user name
        var orgId = _jwtService.GetOrganizationId();  // Tenant ID
        
        // Use these values
    }
}
```

---

## 📤 Response Format

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "statusCode": 200,
  "timestamp": "2025-12-10T10:30:00Z",
  "traceId": "abc123",
  "data": { /* your data */ }
}
```

---

## 🔌 Infrastructure Services Registration

```csharp
// Blob Storage
builder.Services.AddBlobStorageServices(builder.Configuration);

// RabbitMQ
builder.Services.AddRabbitMQ(builder.Configuration);

// Hangfire
builder.Services.AddHangfireServices(builder.Configuration);

// JWT Service
builder.Services.AddJwtService();

// All CQRS Handlers
builder.Services.AddCqrsHandlers(typeof(Program).Assembly);

// All Repositories
builder.Services.AddRepositories(typeof(Program).Assembly);
```

---

## 📋 Middleware Pipeline Order

```csharp
var app = builder.Build();

// 1. Exception handling (must be first)
app.UseGlobalExceptionHandler();

// 2. HTTPS
app.UseHttpsRedirection();

// 3. Static files
app.UseStaticFiles();

// 4. Routing
app.UseRouting();

// 5. Authentication/Authorization
app.UseAuthentication();
app.UseAuthorization();

// 6. Custom middleware
app.UseMultiTenancyMiddleware();  // If using MMO.Core.Middlewares

// 7. Endpoints
app.MapEndpoints();

app.Run();
```

---

## 🧪 Test Pattern

```csharp
[Fact]
public async Task CreateBrand_WithValidData_ReturnsCreatedBrand()
{
    // Arrange
    var command = new CreateBrandCommand { Name = "Test" };
    var handler = new CreateBrandHandler(_mockRepository, _mockLogger);
    
    // Act
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test", result.Name);
}
```

**Naming**: `MethodName_Scenario_ExpectedResult`

---

## ⚙️ Configuration Sections

```json
{
  "Jwt": {
    "SecretKey": "...",
    "Issuer": "...",
    "Audience": "...",
    "ExpirationMinutes": 60
  },
  "BlobStorage": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "MaxFileSizeMB": 100
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest"
  },
  "Hangfire": {
    "Enabled": true,
    "StorageType": "SqlServer",
    "ConnectionString": "..."
  }
}
```

---

## 🚫 Common Anti-Patterns (Avoid These!)

| ❌ Don't | ✅ Do |
|---------|------|
| Put business logic in services | Put business logic in entities |
| Throw `new Exception()` | Throw domain-specific exceptions |
| Use `IHttpContextAccessor` directly | Use `IJwtService` |
| Access `DbContext` from handlers | Use repositories/unit of work |
| Manual handler registration | Use `AddCqrsHandlers()` |
| Mix concerns in single handler | One handler per command/query |
| Hard-code configuration | Use `IOptions<T>` |
| Forget audit trail fields | Always use `BaseEntity<T>` |

---

## 📊 Package Dependencies

```
Minimal:
  MMO.Core.Share
  MMO.Core.Dtos

Core:
  MMO.Core.Entity → depends on → MMO.Core.Share
  MMO.Core.Exceptions → minimal dependencies
  MMO.Core.Contracts → depends on → MMO.Core.Entity

Services:
  MMO.Core.Services → depends on → MMO.Core.Middlewares
  MMO.Core.Extensions → depends on → DKNet.SlimBus, FluentResults

Infrastructure:
  MMO.Core.BlobStorage → depends on → Minio SDK
  MMO.Core.RabbitMQ → depends on → RabbitMQ.Client
  MMO.Core.Hangfire → depends on → Hangfire
```

---

## 🎯 Feature Implementation Checklist

When adding a new feature:

- [ ] Create domain entity inheriting `BaseEntity<T>` or `OwnerOrganizationEntity<T>`
- [ ] Create DTOs: `[Feature]Dto`, `[Feature]ResponseDto` if needed
- [ ] Create domain exception if special error handling needed
- [ ] Create `I[Feature]Repository` interface
- [ ] Implement `[Feature]Repository` class
- [ ] Create `[Feature]Command` (create/update) and `[Feature]Query` (read)
- [ ] Create `[Feature]Handler` for command and query
- [ ] Create `[Feature]Endpoints` with route mapping
- [ ] Add unit tests in `[Feature].UnitTest`
- [ ] Document in README if cross-cutting

---

## 🔗 Inter-Layer Communication

```
API Endpoint
    ↓ (invokes via DI)
CQRS Handler
    ↓ (injects)
IJwtService, IRepository<T>, IService, ILogger
    ↓ (uses)
Domain Entity, Domain Events, Exceptions
    ↓ (persists via)
IRepository<T>
```

---

## 📚 Documentation Files in Codebase

- `MMO.Core.BlobStorage/ARCHITECTURE-DIAGRAM.md` - Blob storage architecture
- `MMO.Core.BlobStorage/QUICK-REFERENCE.md` - Blob storage quick start
- `MMO.Core.BlobStorage/IMPLEMENTATION-SUMMARY.md` - What's included
- `MMO.Core.Dtos/RESPONSE-DTOS-GUIDE.md` - Response DTO usage
- `MMO.Core.Dtos/BASEDENDPOINTS-INTEGRATION.md` - Endpoint integration
- `MMO.Core.Services/JWT-SERVICE-DOCUMENTATION.md` - JWT service docs
- `MMO.Core.Exceptions/README.md` - Exception handling
- `MMO.Core.RabbitMQ/README.md` - Message broker setup
- `MMO.Core.Hangfire/README.md` - Background jobs setup

---

## 🔑 Key Files to Understand First

1. **MMO.Core.Entity/Entities/BasedEntity.cs** - Base entity pattern
2. **MMO.Core.Extensions/BasedEndpoints.cs** - CQRS endpoint mapping
3. **MMO.Core.Extensions/CqrsExtension.cs** - Handler auto-discovery
4. **MMO.Core.Exceptions/Base/DomainException.cs** - Exception hierarchy
5. **MMO.Core.Dtos/*.cs** - Response DTO patterns

---

## 🚀 Getting Started

1. Clone the repository
2. Open `Framework.sln` in Visual Studio or Rider
3. Read [ARCHITECTURE-ANALYSIS.md](./ARCHITECTURE-ANALYSIS.md) for detailed breakdown
4. Explore existing implementations (e.g., `MMO.Core.BlobStorage`)
5. Follow patterns when adding new features
6. Run unit tests: `dotnet test`

---

## ❓ Quick Q&A

**Q: Where do I put business logic?**  
A: In domain entities, not services.

**Q: How do I add a new repository?**  
A: Create interface `I[Entity]Repository` and implementation. Auto-discovered.

**Q: How do I handle errors?**  
A: Throw domain exception. Middleware auto-maps to HTTP response.

**Q: How do I access current user?**  
A: Inject `IJwtService` and use `GetUserId()`, `GetUserName()`.

**Q: How do I add a new endpoint?**  
A: Create CQRS command/query, handler, then map with `MapCqrsPost<>()`.

**Q: Do I need to register repositories manually?**  
A: No, use `AddRepositories()` extension - auto-discovered by convention.

**Q: How is multi-tenancy handled?**  
A: Inherit `OwnerOrganizationEntity<T>`. Queries auto-filter by `OrganizationId`.

**Q: How do I implement soft deletes?**  
A: Inherit `BaseEntity<T>`. Use `SoftDelete(userId)` method. `IsDeleted` automatic.

---

## 📖 Version Info

- **Framework**: .NET 10.0
- **Pattern Version**: Clean Architecture + DDD + CQRS (as of Dec 2025)
- **Total Projects**: 33
- **Test Coverage**: Each project has corresponding UnitTest project
