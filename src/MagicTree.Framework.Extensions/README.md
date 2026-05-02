# MagicTree.Framework.Extensions

Shared extension methods for all MMO microservices.

## Features

### 1. Endpoint Auto-Discovery

Automatically discovers and maps all API endpoints following the `Map*Endpoints` convention.

**Usage**:
```csharp
using MagicTree.Framework.Extensions;

// In Program.cs
app.MapAllEndpoints("YourApi.Api.ApiEndpoints");
```

**Convention Requirements**:
- Endpoint classes must be static
- Methods must be named `Map*Endpoints` (e.g., `MapUserEndpoints`)
- Methods must have signature: `public static void Map*Endpoints(this IEndpointRouteBuilder app)`

**Example**:
```csharp
namespace Auth.Api.ApiEndpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users", () => { /* ... */ });
    }
}
```

See `Apis/Auth/AUTO-ENDPOINT-DISCOVERY.md` for complete documentation.

### 2. Auto-Migration

Automatically applies pending Entity Framework Core migrations when the application starts, controlled by the `AutoMigration` feature flag.

**Usage**:
```csharp
using MagicTree.Framework.Extensions;

// In Program.cs
await app.UseMigrationAsync<YourDbContext>();
```

**Example**:
```csharp
using Auth.Infra.Data;
using MagicTree.Framework.Extensions;

// Apply migrations for AuthDbContext
await app.UseMigrationAsync<AuthDbContext>();
```

**Feature Flag** (in `appsettings.json`):
```json
{
  "FeatureManagement": {
    "AutoMigration": true
  }
}
```

See `Apis/Auth/AUTO-MIGRATION.md` for complete documentation.

## Dependencies

- **Microsoft.AspNetCore.App** (FrameworkReference) - For `IEndpointRouteBuilder` and ASP.NET Core types
- **Microsoft.EntityFrameworkCore** - For `DbContext` base type
- **Microsoft.EntityFrameworkCore.Relational** - For migration extension methods
- **Microsoft.FeatureManagement.AspNetCore** - For feature flag support

## Usage in APIs

Add project reference in your API's `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\Core\MagicTree.Framework.Extensions\MagicTree.Framework.Extensions.csproj" />
</ItemGroup>
```

## Target Framework

.NET 10.0
