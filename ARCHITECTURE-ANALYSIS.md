# MMO.Core Framework - Comprehensive Architectural Analysis

**Analysis Date**: December 2025  
**Framework Target**: .NET 10.0  
**Project Type**: Microservices Shared Framework (Core Packages)

---

## 📋 Table of Contents

1. [Project Structure Overview](#project-structure-overview)
2. [Architectural Patterns](#architectural-patterns)
3. [Layered Architecture](#layered-architecture)
4. [Naming Conventions](#naming-conventions)
5. [CQRS Implementation](#cqrs-implementation)
6. [Repository & Data Access Patterns](#repository--data-access-patterns)
7. [API Endpoint Organization](#api-endpoint-organization)
8. [Exception Handling Strategy](#exception-handling-strategy)
9. [Cross-Cutting Concerns](#cross-cutting-concerns)
10. [Domain Models & Aggregates](#domain-models--aggregates)
11. [Infrastructure Packages](#infrastructure-packages)
12. [Dependency Injection Patterns](#dependency-injection-patterns)

---

## Project Structure Overview

### Solution Layout

```
Framework/
├── Framework.sln
├── README.md
├── LICENSE
└── src/
    ├── Core Layer (Domain Models, DTOs, Contracts)
    │   ├── MMO.Core.Entity/              # Base entities + audit
    │   ├── MMO.Core.Share/               # Shared constants
    │   ├── MMO.Core.Dtos/                # Data transfer objects
    │   ├── MMO.Core.Contracts/           # Event contracts
    │   └── MMO.Core.Exceptions/          # Domain exceptions
    │
    ├── Application/Service Layer
    │   ├── MMO.Core.Services/            # Business logic services
    │   ├── MMO.Core.Extensions/          # Helper extensions
    │   ├── MMO.Core.CodeGenerator/       # Code generation
    │   └── MMO.Core.Configs/             # Configuration helpers
    │
    ├── Infrastructure Layer (Storage & Communication)
    │   ├── MMO.Core.BlobStorage/         # GUID-based file storage (MinIO)
    │   ├── MMO.Core.VideoStorage/        # Video streaming storage
    │   ├── MMO.Core.RabbitMQ/            # Message broker (publisher confirms, DLQ)
    │   ├── MMO.Core.Hangfire/            # Background job processing
    │   ├── MMO.Core.Email/               # Email service with templates
    │   └── MMO.Core.SignalR/             # Real-time communication
    │
    ├── Cross-Cutting Concerns
    │   ├── MMO.Core.Middlewares/         # JWT, MultiTenancy, SaveChange, NullResponse
    │   ├── MMO.Core.RateLimit/           # API throttling/rate limiting
    │   ├── MMO.Core.Idempotency/         # Idempotent request handling
    │   ├── MMO.Core.HybridCache/         # Distributed caching abstraction
    │   ├── MMO.Core.Metrics/             # Observability/metrics
    │   └── MMO.Core.Command/             # CQRS command abstractions
    │       MMO.Core.Queries/             # CQRS query abstractions
    │
    └── Test Projects (33% of solution)
        ├── MMO.Core.*.UnitTest/          # Unit tests for each core package
```

### Key Statistics

- **Total Projects**: 33 (24 core + services, 9 test projects)
- **Target Framework**: .NET 10.0
- **Package Prefix**: `MMO.Core.*` (standardized)
- **Test Suffix**: `.UnitTest` (standardized)
- **Test Location**: Same `src/` directory as implementation

---

## Architectural Patterns

### 1. **Clean Architecture** ✅

The framework implements a strict **layered clean architecture**:

```
Presentation/API Layer
        ↓
Application Layer (CQRS Handlers, Services)
        ↓
Domain/Business Logic Layer (Entities, Value Objects)
        ↓
Infrastructure Layer (Repositories, External Services)
```

**Key Principle**: Dependencies point inward; outer layers depend on inner layers, never reverse.

### 2. **Domain-Driven Design (DDD)** ✅

- **Entities**: Base classes with identity (`BaseEntity<T>`)
- **Aggregates**: Multi-tenant aware (`OwnerOrganizationEntity<T>`)
- **Value Objects**: DTOs represent immutable values
- **Events**: Domain events for integration (`BaseAuthEvent`)
- **Exception Strategy**: Domain-specific exception hierarchy

### 3. **CQRS (Command Query Responsibility Segregation)** ✅

- **Library**: DKNet.SlimBus (handles routing)
- **Pattern**: Separate command and query handlers
- **Auto-Discovery**: Reflection-based handler registration
- **Endpoint Mapping**: Fluent API for route definition

### 4. **Repository Pattern** ✅

- **Convention-based**: Any class implementing `I*Repository` is auto-registered
- **Scope**: All repositories registered as scoped services
- **Support**: Both generic and domain-specific repositories
- **Unit of Work**: `IUnitOfWork` auto-discovered and registered

### 5. **Service-Oriented Architecture** ✅

- **Abstraction-first**: All services defined as interfaces
- **Dependency Injection**: Constructor-based injection throughout
- **Composition**: Services composed in handlers and middleware

---

## Layered Architecture

### Layer 1: Domain Model Layer

**Package**: `MMO.Core.Entity`

#### Base Entity Structure

```csharp
public class BaseEntity<T> : FlagMigrationEntity
{
    // Identity
    public T Id { get; set; }
    
    // Audit Trail
    public DateTimeOffset CreatedOn { get; set; }
    public string CreatedBy { get; set; }
    public string CreatedByName { get; set; }
    
    public DateTimeOffset? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }
    public string? UpdatedByName { get; set; }
    
    // Soft Delete
    public bool IsDeleted { get; set; }
    
    // Audit Methods
    public void SetCreated(string userId, string userName)
    public void SetUpdated(string userId)
    public void SoftDelete(string userId)
    public void Restore(string userId)
}
```

#### Multi-Tenant Entity

```csharp
public abstract class OwnerOrganizationEntity<T> : BaseEntity<T>
{
    /// Organization/Tenant ID for automatic data isolation
    public Guid OrganizationId { get; set; }
    
    public void SetOrganization(Guid organizationId)
}
```

**Key Features**:
- ✅ Generic type support (`T` can be `Guid`, `int`, `string`, etc.)
- ✅ Automatic audit trail (CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
- ✅ Soft delete support with restore capability
- ✅ Multi-tenancy built-in with `OwnerOrganizationEntity<T>`
- ✅ Migration tracking via `FlagMigrationEntity`

---

### Layer 2: DTO & Contract Layer

**Packages**: `MMO.Core.Dtos`, `MMO.Core.Contracts`, `MMO.Core.Share`

#### DTO Structure

```csharp
// Base DTO with audit info
public class BasedDto
{
    public DateTimeOffset? CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }
}

// Standardized response
public record SuccessResponseDto : BasedResponseDto
{
    public DateTime Timestamp { get; set; }
    
    public static SuccessResponseDto Ok(string message = "...")
    public static SuccessResponseDto Created(string message = "...")
    public static SuccessResponseDto Accepted(string message = "...")
}

public record ErrorResponseDto : BasedResponseDto
{
    public string? ErrorCode { get; set; }
    public List<string> Errors { get; set; }
    public Dictionary<string, List<string>> FieldErrors { get; set; }
}
```

#### Event Contracts

**Location**: `MMO.Core.Contracts/Events/`

```
Events/
├── Auth/
│   ├── BaseAuthEvent.cs
│   ├── AccountLockedEvent.cs
│   ├── PasswordResetSuccessEvent.cs
│   ├── TwoFactorStatusChangedEvent.cs
│   └── ... (domain-specific events)
└── [Other domains]/
```

**Pattern**: Events inherit from domain-specific base classes (`BaseAuthEvent`, etc.)

#### Shared Constants

**Location**: `MMO.Core.Share/ShareKey.cs`

```csharp
public static class ShareKey
{
    public const string SystemUser = "SYSTEM";
    // Other shared constants
}
```

---

### Layer 3: Application/Service Layer

**Packages**: `MMO.Core.Services`, `MMO.Core.Extensions`, `MMO.Core.Command`, `MMO.Core.Queries`

#### CQRS Handler Pattern

```csharp
// Command Handler Example
public class CreateBrandHandler : IHandler<CreateBrandRequest, BrandDto>
{
    private readonly IRepository<Brand> _repository;
    private readonly IJwtService _jwtService;
    
    public async Task<BrandDto> Handle(CreateBrandRequest command, CancellationToken ct)
    {
        var userId = _jwtService.GetUserId();
        var brand = new Brand(command.Name, userId);
        await _repository.AddAsync(brand, ct);
        return brand.ToDto();
    }
}

// Query Handler Example
public class GetBrandHandler : IHandler<GetBrandQuery, BrandDto?>
{
    public async Task<BrandDto?> Handle(GetBrandQuery query, CancellationToken ct)
    {
        var brand = await _repository.GetByIdAsync(query.BrandId, ct);
        return brand?.ToDto();
    }
}
```

#### JWT Service

```csharp
public interface IJwtService
{
    JwtInfo? GetJwtInfo();
    string? GetUserId();
    string? GetUserName();
    Guid? GetOrganizationId();
}
```

**Usage**: Injected into handlers to access current user info without HTTP context dependency.

#### Helper Extensions

**BasedEndpoints** - Fluent endpoint mapping:

```csharp
group.MapCqrsPost<CreateBrandCommand, BrandDto>("/brands")
group.MapCqrsGet<GetBrandQuery, BrandDto>("/brands/{brandId}")
group.MapCqrsGet<ListBrandsQuery, List<BrandDto>>("/brands")
```

---

### Layer 4: Infrastructure Layer

#### 4.1 Blob Storage Service

**Package**: `MMO.Core.BlobStorage`

**Pattern**: GUID-based blob storage with MinIO backend

```csharp
public interface IBlobStorageService
{
    Task<BlobMetadata> UploadAsync(BlobUploadRequest request, CancellationToken ct);
    Task<BlobDownloadResult?> DownloadAsync(Guid blobId, CancellationToken ct);
    Task<bool> DeleteAsync(Guid blobId, CancellationToken ct);
    Task<bool> ExistsAsync(Guid blobId, CancellationToken ct);
    Task<BlobMetadata?> GetMetadataAsync(Guid blobId, CancellationToken ct);
    Task<string?> GetPresignedUrlAsync(Guid blobId, int? expirationHours, CancellationToken ct);
}
```

**Key Features**:
- Automatic GUID generation for unique file identification
- File naming: `{GUID}{extension}` (e.g., `123e4567-e89b-12d3-a456-426614174000.jpg`)
- Metadata storage in object headers
- Presigned URLs for temporary direct access
- Maximum file size enforcement
- File extension whitelist validation

#### 4.2 Message Broker

**Package**: `MMO.Core.RabbitMQ`

**Features**:
- Publisher confirms for guaranteed delivery
- Automatic retry with exponential backoff
- Dead letter queues (DLQ) for failed messages
- Connection recovery and topology recovery
- Health monitoring background service
- Prefetch control for throughput tuning

#### 4.3 Background Job Processing

**Package**: `MMO.Core.Hangfire`

**Job Types**:
- Fire-and-forget (immediate)
- Delayed (scheduled for later)
- Recurring (cron-based scheduling)
- Continuation (chained jobs)

**Storage Options**: SQL Server (production) or InMemory (development)

**Features**:
- Dashboard UI at `/hangfire`
- Automatic retries with exponential backoff
- Role-based dashboard authorization
- Worker pool configuration

#### 4.4 Email Service

**Package**: `MMO.Core.Email`

**Structure**:
```
EmailTemplates/
├── PasswordReset.html
├── VerifyEmail.html
└── ... (template files)
```

#### 4.5 Video Storage

**Package**: `MMO.Core.VideoStorage`

Similar abstraction to BlobStorage, optimized for video streaming.

---

### Layer 5: Exception Handling Layer

**Package**: `MMO.Core.Exceptions`

#### Exception Hierarchy

```
DomainException (abstract base)
├── EntityNotFoundException<TKey>        → HTTP 404
├── EntityAlreadyExistsException        → HTTP 409
├── EntityValidationException           → HTTP 400
├── InvalidEntityOperationException     → HTTP 400
└── UnauthorizedEntityAccessException   → HTTP 403
```

#### Usage Pattern

```csharp
// Throw in domain logic
if (user == null)
    throw new EntityNotFoundException<Guid>("User", userId);

// Global middleware catches and maps to HTTP response
app.UseGlobalExceptionHandler();
```

#### HTTP Mapping

- **DomainException** → Auto-mapped to appropriate HTTP status code
- **Response Format**: Structured JSON with error codes and metadata

---

## Naming Conventions

### Namespace Pattern

**Principle**: Hierarchical, feature-based organization

```
MMO.Core.[Feature]
├── [Feature]/[SubDomain]/ClassName.cs
└── Example: MMO.Core.Services.Jwt.JwtService.cs
```

### Class Naming

| Element | Pattern | Example |
|---------|---------|---------|
| **Domain Entity** | `[Domain]Entity` or `Base[Domain]Entity` | `UserEntity`, `BrandEntity` |
| **DTO** | `[Domain]Dto` | `UserDto`, `BrandDto` |
| **Response DTO** | `[Domain]ResponseDto` | `UserResponseDto` |
| **Info DTO** | `[Domain]InfoDto` | `UserInfoDto` |
| **Service Interface** | `I[Service]Service` | `IUserService`, `IJwtService` |
| **Service Implementation** | `[Service]Service` | `UserService`, `JwtService` |
| **Command Handler** | `[Command]Handler` | `CreateUserHandler`, `UpdateBrandHandler` |
| **Query Handler** | `[Query]Handler` | `GetUserHandler`, `ListBrandsHandler` |
| **Repository Interface** | `I[Entity]Repository` | `IUserRepository`, `IBrandRepository` |
| **Repository Implementation** | `[Entity]Repository` | `UserRepository`, `BrandRepository` |
| **Event** | `[Action][Entity]Event` | `UserCreatedEvent`, `PasswordResetEvent` |
| **Exception** | `[Specific]Exception` | `UserNotFoundException`, `InvalidBrandException` |
| **Extension Class** | `[Domain]Extension` or `[Domain]Extensions` | `UserExtensions`, `DateTimeExtensions` |
| **Middleware** | `[Function]Middleware` | `ExceptionHandlingMiddleware`, `JwtMiddleware` |
| **Enum** | `[Name]Enum` or `[Name]s` | `UserStatusEnum`, `Priorities` |

### Project Naming

```
Core Layer:        MMO.Core.[Feature]
Test Layer:        MMO.Core.[Feature].UnitTest
ShareAppHost:      MMO.Core.ShareAppHost, MMO.Core.ShareKey
```

### Property Naming

- **Public Properties**: PascalCase (standard C# convention)
- **Private Fields**: `_camelCase`
- **Constants**: `UPPER_SNAKE_CASE` or PascalCase (enum values)

### Method Naming

```csharp
// Async methods: *Async suffix
public async Task<T> GetByIdAsync(Guid id, CancellationToken ct)
public async Task AddAsync(T entity, CancellationToken ct)

// Query methods: Get*, Find*, List*
GetById(id), GetByEmail(email), FindById(id), ListAll()

// Command methods: Create*, Update*, Delete*, Add*, Remove*
CreateUser(), UpdateBrand(), DeleteProduct()

// State checking: Is*, Has*, Can*
IsActive, HasPermission(), CanDelete()

// Event handlers: On*
OnUserCreated(), OnPaymentReceived()

// Conversions: To*
ToDto(), ToEntity(), ToViewModel()
```

---

## CQRS Implementation

### Architecture Pattern

```
API Endpoint
    ↓
Route Handler (MapCqrsPost, MapCqrsGet)
    ↓
CQRS Command/Query Object
    ↓
DKNet.SlimBus Dispatcher
    ↓
Command/Query Handler (IHandler<T, TResponse>)
    ↓
Repository / Service Layer
    ↓
Database / External Service
```

### Command Handler Template

```csharp
using DKNet.Fluents.Requests;
using MMO.Core.Services;

public class CreateBrandCommand : IWitResponse<BrandDto>
{
    public string Name { get; set; }
    public string? Description { get; set; }
}

public class CreateBrandHandler : IHandler<CreateBrandCommand, BrandDto>
{
    private readonly IRepository<Brand> _repository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<CreateBrandHandler> _logger;
    
    public CreateBrandHandler(
        IRepository<Brand> repository,
        IJwtService jwtService,
        ILogger<CreateBrandHandler> logger)
    {
        _repository = repository;
        _jwtService = jwtService;
        _logger = logger;
    }
    
    public async Task<BrandDto> Handle(CreateBrandCommand command, CancellationToken ct)
    {
        var userId = _jwtService.GetUserId();
        _logger.LogInformation("Creating brand: {Name}", command.Name);
        
        var brand = new Brand(command.Name, userId);
        await _repository.AddAsync(brand, ct);
        
        return brand.ToDto();
    }
}
```

### Query Handler Template

```csharp
using DKNet.Fluents.Queries;

public class GetBrandQuery : IWitResponse<BrandDto>
{
    [FromRoute]
    public Guid BrandId { get; set; }
}

public class GetBrandHandler : IHandler<GetBrandQuery, BrandDto?>
{
    private readonly IRepository<Brand> _repository;
    
    public async Task<BrandDto?> Handle(GetBrandQuery query, CancellationToken ct)
    {
        var brand = await _repository.GetByIdAsync(query.BrandId, ct);
        
        if (brand == null)
            throw new EntityNotFoundException<Guid>("Brand", query.BrandId);
        
        return brand.ToDto();
    }
}
```

### Auto-Discovery Registration

```csharp
// Program.cs
builder.Services.AddCqrsHandlers(typeof(Program).Assembly);
// OR
builder.Services.AddCqrsHandlersFromAssemblyContaining(typeof(CreateBrandHandler));
```

**How it works**:
1. Scans assembly for all classes
2. Finds classes implementing `IHandler<TRequest, TResponse>`
3. Registers as scoped services automatically

---

## Repository & Data Access Patterns

### Repository Interface Convention

```csharp
public interface IBrandRepository
{
    Task<Brand?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Brand?> GetByNameAsync(string name, CancellationToken ct);
    Task<List<Brand>> ListAllAsync(CancellationToken ct);
    Task AddAsync(Brand entity, CancellationToken ct);
    Task UpdateAsync(Brand entity, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
```

### Generic Repository Pattern

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id, CancellationToken ct);
    Task<List<T>> GetAllAsync(CancellationToken ct);
    Task AddAsync(T entity, CancellationToken ct);
    Task UpdateAsync(T entity, CancellationToken ct);
    Task DeleteAsync(object id, CancellationToken ct);
}
```

### Auto-Discovery Registration

```csharp
// Program.cs
builder.Services.AddRepositories(typeof(Program).Assembly);
// OR specific to handlers assembly
builder.Services.AddRepositoriesFromAssemblyContaining(typeof(CreateBrandHandler));
```

**Convention**: Any class implementing `I*Repository` is auto-registered as **scoped**.

### Multi-Tenancy in Repositories

**Pattern**: Automatic filtering by `OrganizationId`

```csharp
// All queries automatically filter:
var brands = await _repository.GetAllAsync(ct);
// Behind the scenes: WHERE OrganizationId = currentUserOrganizationId
```

---

## API Endpoint Organization

### Minimal APIs Pattern

```csharp
var brandGroup = app.MapGroup("/api/brands")
    .WithName("Brands")
    .WithOpenApi()
    .WithTags("Brand Management");

// Command endpoint
brandGroup.MapCqrsPost<CreateBrandCommand, BrandDto>("")
    .WithName("Create Brand")
    .WithDescription("Creates a new brand");

// Query endpoint (single)
brandGroup.MapCqrsGet<GetBrandQuery, BrandDto>("{brandId}")
    .WithName("Get Brand")
    .WithDescription("Retrieves brand details");

// Query endpoint (list)
brandGroup.MapCqrsGet<ListBrandsQuery, List<BrandDto>>("")
    .WithName("List Brands")
    .WithDescription("Retrieves all brands");

// Command endpoint (update)
brandGroup.MapCqrsPut<UpdateBrandCommand, BrandDto>("{brandId}")
    .WithName("Update Brand")
    .WithDescription("Updates brand details");

// Command endpoint (delete)
brandGroup.MapCqrsDelete<DeleteBrandCommand, bool>("{brandId}")
    .WithName("Delete Brand")
    .WithDescription("Deletes a brand");
```

### Response Wrapper Pattern

**All responses wrapped in standardized DTOs**:

```json
// Success Response
{
  "success": true,
  "message": "Brand created successfully",
  "statusCode": 201,
  "timestamp": "2025-12-10T10:30:00Z",
  "traceId": "0HN1GH7VJ2K3L:00000001",
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Acme Corp",
    "description": "...",
    "createdOn": "2025-12-10T10:30:00Z",
    "createdBy": "user123"
  }
}

// Error Response
{
  "success": false,
  "message": "Validation failed",
  "statusCode": 400,
  "timestamp": "2025-12-10T10:30:00Z",
  "traceId": "0HN1GH7VJ2K3L:00000001",
  "errorCode": "VALIDATION_ERROR",
  "errors": [
    "Name: Name is required",
    "Description: Max length is 500"
  ],
  "metadata": {
    "fieldErrors": {
      "Name": ["Name is required"],
      "Description": ["Max length is 500"]
    }
  }
}
```

### OpenAPI/Scalar Documentation

- Automatic from route definitions
- Support for `.WithOpenApi()`, `.Produces()`, `.Accepts()`
- Scalar UI integration via `MMO.Core.Configs`

---

## Exception Handling Strategy

### Domain Exception Inheritance

```csharp
// Abstract base
public abstract class DomainException : Exception
{
    public string ErrorCode { get; protected set; }
    public object? Details { get; protected set; }
}

// Specific implementations
public class EntityNotFoundException<TKey> : DomainException
{
    // Maps to HTTP 404
}

public class EntityAlreadyExistsException : DomainException
{
    // Maps to HTTP 409 (Conflict)
}

public class EntityValidationException : DomainException
{
    // Maps to HTTP 400 (Bad Request)
}

public class InvalidEntityOperationException : DomainException
{
    // Maps to HTTP 400 (Bad Request)
}

public class UnauthorizedEntityAccessException : DomainException
{
    // Maps to HTTP 403 (Forbidden)
}
```

### Global Exception Middleware

```csharp
// Program.cs
using MMO.Core.Exceptions.Extensions;

app.UseGlobalExceptionHandler();  // Place before auth
app.UseAuthentication();
app.UseAuthorization();
```

**How it works**:
1. Catches all unhandled exceptions
2. If `DomainException`, maps to appropriate HTTP status
3. Returns structured error response
4. If other exception, returns 500 with generic message
5. Logs full stack trace for debugging

---

## Cross-Cutting Concerns

### 1. Multi-Tenancy (MMO.Core.Middlewares)

```csharp
// Automatic tenant isolation
public abstract class OwnerOrganizationEntity<T> : BaseEntity<T>
{
    public Guid OrganizationId { get; set; }
}

// All queries automatically filtered by OrganizationId
// Unless user has "Host" role
```

### 2. Audit Trail (Built into BaseEntity)

```csharp
// Every entity tracks:
public DateTimeOffset CreatedOn { get; set; }
public string CreatedBy { get; set; }
public string CreatedByName { get; set; }
public DateTimeOffset? UpdatedOn { get; set; }
public string? UpdatedBy { get; set; }

// Set via methods:
entity.SetCreated(userId, userName);
entity.SetUpdated(userId);
```

### 3. Soft Deletes

```csharp
// Entities can be soft-deleted, not permanently removed
public bool IsDeleted { get; set; }

entity.SoftDelete(userId);  // Mark as deleted
entity.Restore(userId);     // Restore if needed
```

### 4. Rate Limiting (MMO.Core.RateLimit)

```csharp
// Middleware-based rate limiting
// Configurable per endpoint
// Supports various strategies (fixed window, sliding window, etc.)
```

### 5. Idempotency (MMO.Core.Idempotency)

```csharp
// Prevents duplicate operations
// Uses idempotency key in request header
// Stores and returns previous response if duplicate detected
```

### 6. Distributed Caching (MMO.Core.HybridCache)

```csharp
// Abstraction over distributed cache
// Supports multiple backends
// Integrated cache invalidation
```

### 7. Request Tracing

```csharp
// Every response includes TraceId
// Correlates logs across services
// Available in HttpContext.TraceIdentifier
```

---

## Domain Models & Aggregates

### Aggregate Root Pattern

```csharp
// Domain Entity (Aggregate Root)
public class Brand : BaseEntity<Guid>
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    
    private List<BrandCategory> _categories = new();
    public IReadOnlyList<BrandCategory> Categories => _categories.AsReadOnly();
    
    // Domain methods (business logic)
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new InvalidEntityOperationException("Brand", "UpdateName", "NameRequired");
        
        Name = newName;
        SetUpdated(userId);
    }
    
    public void AddCategory(BrandCategory category)
    {
        if (_categories.Any(c => c.Id == category.Id))
            throw new InvalidEntityOperationException("Brand", "AddCategory", "CategoryAlreadyAdded");
        
        _categories.Add(category);
    }
}

// Value Object (Child of Aggregate)
public class BrandCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}
```

### DTO Conversion Pattern

```csharp
// Fluent mapping
public static BrandDto ToDto(this Brand brand)
{
    return new BrandDto
    {
        Id = brand.Id,
        Name = brand.Name,
        Description = brand.Description,
        CreatedOn = brand.CreatedOn,
        CreatedBy = brand.CreatedBy,
        CreatedByName = brand.CreatedByName,
        UpdatedOn = brand.UpdatedOn,
        UpdatedBy = brand.UpdatedBy
    };
}
```

---

## Infrastructure Packages

### Package Dependency Graph

```
MMO.Core.Dtos
    ├─ (no dependencies)

MMO.Core.Share
    ├─ (no dependencies)

MMO.Core.Entity
    ├─ MMO.Core.Share

MMO.Core.Exceptions
    ├─ (minimal: HTTP abstractions only)

MMO.Core.Contracts
    ├─ MMO.Core.Entity

MMO.Core.Services
    ├─ MMO.Core.Middlewares (for JWT)

MMO.Core.Extensions
    ├─ DKNet.SlimBus.Extensions (CQRS)
    ├─ FluentResults
    ├─ Microsoft.AspNetCore.OpenApi

MMO.Core.Middlewares
    ├─ MMO.Core.Exceptions

MMO.Core.BlobStorage
    ├─ MMO.Core.Dtos
    ├─ Minio SDK

MMO.Core.RabbitMQ
    ├─ RabbitMQ.Client

MMO.Core.Hangfire
    ├─ Hangfire.Core
    ├─ Hangfire.SqlServer

[Other Infrastructure Packages]
    ├─ Specific SDKs (MinIO, RabbitMQ, etc.)
```

### Feature Enablement Pattern

**Configuration-driven feature flags**:

```json
{
  "Hangfire": {
    "Enabled": true
  },
  "RabbitMQ": {
    "Enabled": true
  }
}
```

---

## Dependency Injection Patterns

### DI Registration Patterns

```csharp
// 1. Automatic CQRS Handler Discovery
builder.Services.AddCqrsHandlers(typeof(Program).Assembly);

// 2. Automatic Repository Discovery
builder.Services.AddRepositories(typeof(Program).Assembly);

// 3. Core Services
builder.Services.AddJwtService();

// 4. Infrastructure Services
builder.Services.AddBlobStorageServices(builder.Configuration);
builder.Services.AddRabbitMQ(builder.Configuration);
builder.Services.AddHangfireServices(builder.Configuration);

// 5. Middleware Pipeline
app.UseGlobalExceptionHandler();
app.UseJwtMiddleware();
app.UseMultiTenancyMiddleware();
```

### Service Lifetimes

| Service Type | Lifetime | Reason |
|--------------|----------|--------|
| CQRS Handlers | **Scoped** | One handler per request |
| Repositories | **Scoped** | DB context is scoped |
| Services | **Scoped/Transient** | Depends on state |
| Middleware | **Singleton** | Reused across requests |
| Configuration | **Singleton** | Immutable configuration |

---

## Code Organization Best Practices

### File Organization

```
Domain/
├── Entities/
│   ├── Brand.cs
│   ├── User.cs
│   └── Product.cs
├── ValueObjects/
│   ├── Money.cs
│   └── Address.cs
├── Events/
│   ├── BrandCreatedEvent.cs
│   └── UserRegisteredEvent.cs
└── Exceptions/
    ├── BrandNotFoundException.cs
    └── InvalidBrandException.cs

Application/
├── Commands/
│   ├── CreateBrandCommand.cs
│   └── UpdateBrandCommand.cs
├── Queries/
│   ├── GetBrandQuery.cs
│   └── ListBrandsQuery.cs
├── Handlers/
│   ├── CreateBrandHandler.cs
│   ├── GetBrandHandler.cs
│   └── ListBrandsHandler.cs
└── Services/
    ├── IBrandService.cs
    └── BrandService.cs

Infrastructure/
├── Repositories/
│   ├── IBrandRepository.cs
│   └── BrandRepository.cs
├── Persistence/
│   ├── BrandDbContext.cs
│   └── BrandConfiguration.cs
└── External/
    ├── EmailService.cs
    └── FileStorageService.cs

Api/
├── Endpoints/
│   └── BrandEndpoints.cs
├── Middleware/
│   └── ErrorHandlingMiddleware.cs
└── Program.cs
```

### Namespace Organization

```csharp
// Domain layer
namespace MyApi.Domain.Entities { }
namespace MyApi.Domain.ValueObjects { }
namespace MyApi.Domain.Events { }
namespace MyApi.Domain.Exceptions { }

// Application layer
namespace MyApi.Application.Commands { }
namespace MyApi.Application.Queries { }
namespace MyApi.Application.Handlers { }
namespace MyApi.Application.Services { }

// Infrastructure layer
namespace MyApi.Infrastructure.Repositories { }
namespace MyApi.Infrastructure.Persistence { }
namespace MyApi.Infrastructure.External { }

// API layer
namespace MyApi.Api.Endpoints { }
namespace MyApi.Api.Middleware { }
```

---

## Testing Strategy

### Test Project Structure

```
[Feature].UnitTest/
├── [Feature]ServiceTests.cs
├── [Feature]HandlerTests.cs
├── [Feature]RepositoryTests.cs
├── Fixtures/
│   └── [Feature]TestFixture.cs
└── TestData/
    └── [Feature]TestData.cs
```

### Test Naming Convention

```csharp
// Format: MethodName_Scenario_ExpectedResult
[Fact]
public async Task CreateBrand_WithValidInput_ReturnsCreatedBrand()
{
    // Arrange
    var command = new CreateBrandCommand { Name = "Test Brand" };
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test Brand", result.Name);
}

[Fact]
public async Task GetBrand_WithNonExistentId_ThrowsNotFoundException()
{
    // Arrange
    var query = new GetBrandQuery { BrandId = Guid.NewGuid() };
    
    // Act & Assert
    await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
        () => _handler.Handle(query, CancellationToken.None));
}
```

---

## Configuration Strategy

### Configuration Sections

```json
{
  "Logging": { },
  "ConnectionStrings": { },
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
    "Password": "guest",
    "PublisherConfirms": true,
    "RetryPolicy": { }
  },
  "Hangfire": {
    "Enabled": true,
    "StorageType": "SqlServer",
    "ConnectionString": "..."
  }
}
```

### Configuration Binding

```csharp
// Program.cs
var jwtOptions = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration missing");

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));
```

---

## Summary: Key Architectural Decisions

| Decision | Pattern | Benefit |
|----------|---------|---------|
| **Layered Architecture** | Clean Architecture | Clear separation of concerns, testability |
| **CQRS Pattern** | Separate read/write paths | Optimizable query/command handling |
| **DDD Principles** | Rich domain models | Business logic in entities, not services |
| **Auto-Discovery DI** | Convention-based registration | Less boilerplate, automatic scaling |
| **Exception Hierarchy** | Domain-specific exceptions | Type-safe error handling, HTTP mapping |
| **Multi-Tenancy Built-in** | Automatic filtering | Data isolation by design |
| **Audit Trail Built-in** | Entity tracking | Compliance, debugging, audit requirements |
| **Soft Deletes** | Logical deletion | Data preservation, restore capability |
| **Service-Oriented** | Interface abstractions | Loose coupling, testability, composition |
| **Minimal APIs** | Route groups + CQRS | Type-safe, concise endpoint definition |
| **Configuration-Driven** | Settings over code | Environment-specific behavior |

---

## Recommendations for New Developers

1. ✅ **Follow naming conventions** - They're intentional and enable auto-discovery
2. ✅ **Inherit from `BaseEntity<T>`** - Gets audit, soft delete, timestamps automatically
3. ✅ **Use `OwnerOrganizationEntity<T>`** - For multi-tenant data
4. ✅ **Create domain exceptions** - Don't rely on generic exceptions
5. ✅ **Put business logic in entities** - Not in services (DDD principle)
6. ✅ **Use CQRS handlers** - One handler per command/query (single responsibility)
7. ✅ **Leverage auto-discovery** - Add repository → it's automatically registered
8. ✅ **Throw domain exceptions** - Global middleware handles conversion to HTTP responses
9. ✅ **Use IJwtService** - To access current user without HTTP context dependency
10. ✅ **Keep infrastructure abstract** - Depend on interfaces, not implementations

---

## Document Metadata

- **Created**: December 2025
- **Framework Version**: .NET 10.0
- **Last Updated**: [Current Date]
- **Maintainer**: Architecture Team
- **Status**: Living Document (Updated as architecture evolves)
