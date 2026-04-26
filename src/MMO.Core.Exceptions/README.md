# MMO.Core.Exceptions

Centralized domain exception handling package for all MMO microservices.

## Overview

This package provides a standardized exception hierarchy and global exception handler middleware for consistent error handling across all 16 APIs.

## Features

- ✅ **Base DomainException** - Abstract base with ErrorCode and Details
- ✅ **Generic Exceptions** - Reusable exceptions for common scenarios
- ✅ **Global Middleware** - Automatic HTTP status mapping
- ✅ **Structured Errors** - JSON responses with error codes and metadata
- ✅ **Type Safety** - Compile-time exception handling

## Exception Hierarchy

```
DomainException (abstract base)
├── EntityNotFoundException<TKey> → HTTP 404
├── EntityAlreadyExistsException → HTTP 409
├── EntityValidationException → HTTP 400
├── InvalidEntityOperationException → HTTP 400
└── UnauthorizedEntityAccessException → HTTP 403
```

## Installation

Add package reference to your API project:

```xml
<ItemGroup>
  <PackageReference Include="MMO.Core.Exceptions" />
</ItemGroup>
```

## Usage

### 1. Register Middleware in Program.cs

```csharp
using MMO.Core.Exceptions.Extensions;

var app = builder.Build();

// Add BEFORE UseAuthentication
app.UseGlobalExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
```

### 2. Throw Exceptions in Domain Entities

```csharp
using MMO.Core.Exceptions.Common;

public class User : BaseEntity<Guid>
{
    public void Activate()
    {
        if (IsActive)
            throw new InvalidEntityOperationException("User", "Activate", "AlreadyActive");
        
        if (!IsEmailVerified)
            throw new InvalidEntityOperationException("User", "Activate", "EmailNotVerified");
        
        IsActive = true;
    }
}
```

### 3. Use in Handlers with FluentResults

```csharp
using MMO.Core.Exceptions.Common;
using FluentResults;

public class GetUserHandler : IHandler<GetUserQuery, UserDto?>
{
    public async Task<UserDto?> Handle(GetUserQuery query, CancellationToken ct)
    {
        var user = await _repository.GetByIdAsync(query.UserId, ct);
        if (user == null)
            throw new EntityNotFoundException<Guid>("User", query.UserId);
        
        return user.ToDto();
    }
}
```

### 4. Create Custom Entity-Specific Exceptions

For entity-specific exceptions, inherit from generic base:

```csharp
using MMO.Core.Exceptions.Common;

namespace YourApi.Domain.Exceptions;

public class UserNotFoundException : EntityNotFoundException<Guid>
{
    public UserNotFoundException(Guid userId) 
        : base("User", userId)
    {
    }
}

public class UserEmailAlreadyExistsException : EntityAlreadyExistsException
{
    public UserEmailAlreadyExistsException(string email) 
        : base("User", "Email", email)
    {
    }
}
```

## HTTP Status Mapping

| Exception Type | HTTP Status | When to Use |
|---------------|-------------|-------------|
| `EntityNotFoundException<T>` | 404 Not Found | Entity not found by ID or property |
| `EntityAlreadyExistsException` | 409 Conflict | Duplicate entity creation |
| `EntityValidationException` | 400 Bad Request | Business rule violation |
| `InvalidEntityOperationException` | 400 Bad Request | Invalid state transition |
| `UnauthorizedEntityAccessException` | 403 Forbidden | Permission denied |

## Response Format

All exceptions return structured JSON:

```json
{
  "error": "USER_NOT_FOUND",
  "message": "User with ID '123e4567-e89b-12d3-a456-426614174000' was not found.",
  "details": {
    "EntityName": "User",
    "EntityId": "123e4567-e89b-12d3-a456-426614174000"
  },
  "timestamp": "2025-12-13T10:30:00Z"
}
```

## Benefits

- ✅ **Consistent Error Handling** - Same pattern across all 16 APIs
- ✅ **Type Safety** - Compile-time exception checking
- ✅ **Rich Context** - Error codes and metadata for debugging
- ✅ **Clean API Responses** - Proper HTTP status codes
- ✅ **Monitoring Ready** - Error codes enable metrics and alerts
- ✅ **Testability** - Easy to unit test exception scenarios

## Dependencies

- Microsoft.AspNetCore.Http.Abstractions (2.2.0)
- Microsoft.Extensions.Logging.Abstractions (10.0.0)

## See Also

- [copilot-instructions.md](../../.github/copilot-instructions.md) - Domain-Specific Exceptions section
- [API-PATTERN-AUDIT.md](../../API-PATTERN-AUDIT.md) - Architecture compliance
