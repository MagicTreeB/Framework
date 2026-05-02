# Auto Save Changes Middleware

Automatically saves DbContext changes after successful POST, PUT, PATCH, and DELETE requests.

## Setup

### 1. Register Services

```csharp
// In Program.cs
using MagicTree.Framework.Middlewares.SaveChange;

var builder = WebApplication.CreateBuilder(args);

// Add auto-save services
builder.Services.AddAutoSaveChanges();

// Configure your DbContext with the tracker interceptor
builder.Services.AddDbContext<YourDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
    
    // Add the interceptor to track DbContext
    var tracker = sp.GetRequiredService<IDbContextTracker>();
    options.AddInterceptors(new DbContextTrackerInterceptor(tracker));
});
```

### 2. Register Middleware

```csharp
// In Program.cs
var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Add auto-save middleware BEFORE endpoints
app.UseAutoSaveChanges();

app.MapControllers(); // or app.MapAllEndpoints()
```

## How It Works

1. **Request Processing**: Middleware executes the request pipeline
2. **Success Check**: Only saves if response status is 200-299
3. **Method Check**: Only saves for POST, PUT, PATCH, DELETE requests
4. **Change Detection**: Checks if DbContext has pending changes
5. **Auto-Save**: Calls `SaveChangesAsync()` on all tracked DbContexts

## Benefits

✅ **Eliminates Boilerplate**: No need to call `SaveChangesAsync()` in every handler  
✅ **Consistent**: All mutating operations automatically persist changes  
✅ **Safe**: Only saves on successful responses  
✅ **Flexible**: Works with multiple DbContexts  
✅ **Performant**: Only saves when there are actual changes  

## Example

### Before (Manual Save):
```csharp
group.MapPost<CreateUserCommand, UserDto>("/")
    .WithMetadata("CreateUser", "Create user");

// In handler
public async Task<IResult<UserDto>> Handle(CreateUserCommand command)
{
    var user = new User { ... };
    _context.Users.Add(user);
    await _context.SaveChangesAsync(); // Manual save
    return Result.Ok(user.ToDto());
}
```

### After (Auto Save):
```csharp
group.MapPost<CreateUserCommand, UserDto>("/")
    .WithMetadata("CreateUser", "Create user");

// In handler
public async Task<IResult<UserDto>> Handle(CreateUserCommand command)
{
    var user = new User { ... };
    _context.Users.Add(user);
    // No manual SaveChangesAsync needed!
    return Result.Ok(user.ToDto());
}
```

## Important Notes

- **GET requests**: Not affected (no auto-save)
- **Failed requests**: Changes are not saved (status code >= 300)
- **Transactions**: You can still use manual transactions when needed
- **Performance**: Use `.AsNoTracking()` for read-only queries to avoid unnecessary tracking
