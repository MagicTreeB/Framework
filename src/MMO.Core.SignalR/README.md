# MMO.Core.SignalR

Standardized SignalR package for real-time communication across all microservices in the MMO platform.

## Features

- ✅ **Base Hub Classes**: Pre-built hub with authentication support
- ✅ **SignalR Service**: High-level service for sending messages without hub injection
- ✅ **Notification Models**: Standard notification message format
- ✅ **Redis Backplane**: Scale-out support for multiple instances
- ✅ **CORS Support**: Configurable cross-origin support
- ✅ **Group Management**: Built-in group join/leave functionality
- ✅ **Progress Updates**: Send progress updates for long-running tasks
- ✅ **User Targeting**: Send messages to specific users or groups
- ✅ **Type-Safe Clients**: Interface-based client contracts

## Installation

The package is already referenced in your project via Central Package Management.

## Configuration

**appsettings.json:**
```json
{
  "SignalR": {
    "Enabled": true,
    "UseRedisBackplane": false,
    "RedisConnectionString": "localhost:6379",
    "EnableDetailedErrors": false,
    "KeepAliveIntervalSeconds": 15,
    "ClientTimeoutIntervalSeconds": 30,
    "MaximumReceiveMessageSize": 32768,
    "StreamingBufferCapacity": 10,
    "EnableReconnect": true,
    "AllowedOrigins": "https://localhost:5173,https://localhost:5174"
  }
}
```

## Usage

### 1. Register Services (Program.cs)

```csharp
using MMO.Core.SignalR.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add SignalR services
builder.Services.AddSignalRService(builder.Configuration);

// Add CORS for SignalR (optional)
builder.Services.AddSignalRCors(builder.Configuration);

var app = builder.Build();

// Use CORS (if configured)
app.UseCors("SignalRCorsPolicy");

// Map SignalR hub
app.UseSignalREndpoints("/hubs/notifications");

app.Run();
```

### 2. Create Custom Hub (Optional)

If you need custom hub methods, inherit from `BaseNotificationHub`:

```csharp
using MMO.Core.SignalR.Hubs;
using MMO.Core.SignalR.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace YourApi.Hubs;

public class CustomHub : BaseNotificationHub
{
    public CustomHub(ILogger<CustomHub> logger) : base(logger) { }

    // Add custom methods
    public async Task SendChatMessage(string message)
    {
        var userId = GetUserId();
        await Clients.All.ReceiveMessage("ChatMessage", new { userId, message });
    }
}
```

### 3. Send Notifications from Application Layer

**Inject ISignalRService into handlers:**

```csharp
using MMO.Core.SignalR.Interfaces;
using MMO.Core.SignalR.Models;

public class CreateOrderHandler : IHandler<CreateOrderCommand, OrderDto>
{
    private readonly ISignalRService _signalR;
    private readonly IOrderRepository _repository;

    public CreateOrderHandler(
        ISignalRService signalR,
        IOrderRepository repository)
    {
        _signalR = signalR;
        _repository = repository;
    }

    public async Task<IResult<OrderDto>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var order = Order.Create(request.CustomerId, request.Items);
        await _repository.AddAsync(order, ct);

        // Send notification to user
        var notification = NotificationMessage.Success(
            "Order Created",
            $"Order #{order.Id} has been created successfully.",
            order.CustomerId.ToString()
        );
        await _signalR.SendNotificationToUserAsync(order.CustomerId.ToString(), notification, ct);

        return Result.Ok(order.ToDto());
    }
}
```

### 4. Client-Side Integration

**JavaScript/TypeScript (React, Vue, Angular):**

```typescript
import * as signalR from "@microsoft/signalr";

// Create connection
const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://localhost:7001/hubs/notifications", {
    accessTokenFactory: () => getAccessToken() // JWT token
  })
  .withAutomaticReconnect()
  .configureLogging(signalR.LogLevel.Information)
  .build();

// Register event handlers
connection.on("ReceiveNotification", (notification) => {
  console.log("Notification:", notification);
  // Display toast/notification in UI
  showNotification(notification.title, notification.message, notification.type);
});

connection.on("ProgressUpdate", (taskId, progress, message) => {
  console.log(`Task ${taskId}: ${progress}% - ${message}`);
  // Update progress bar in UI
  updateProgressBar(taskId, progress, message);
});

connection.on("ReceiveMessage", (method, message) => {
  console.log(`Message via ${method}:`, message);
});

// Start connection
await connection.start();
console.log("SignalR Connected");

// Join a group
await connection.invoke("JoinGroup", "admin-users");

// Get connection ID
const connectionId = await connection.invoke("GetConnectionId");
console.log("Connection ID:", connectionId);
```

**Install client library:**
```bash
npm install @microsoft/signalr
```

## API Reference

### ISignalRService Methods

**Notifications:**
```csharp
// Broadcast to all clients
await _signalR.SendNotificationToAllAsync(notification);

// Send to specific user
await _signalR.SendNotificationToUserAsync("user-id", notification);

// Send to group
await _signalR.SendNotificationToGroupAsync("admin-users", notification);

// Send to multiple users
await _signalR.SendNotificationToUsersAsync(new[] { "user1", "user2" }, notification);
```

**Custom Messages:**
```csharp
// Broadcast custom message
await _signalR.SendMessageToAllAsync("UpdateDashboard", dashboardData);

// Send to user
await _signalR.SendMessageToUserAsync("user-id", "OrderUpdate", orderData);

// Send to group
await _signalR.SendMessageToGroupAsync("admin-users", "SystemAlert", alertData);
```

**Progress Updates:**
```csharp
// Send progress update to user
await _signalR.SendProgressUpdateAsync("user-id", "import-task-123", 50, "Processing records...");
```

**Group Management:**
```csharp
// Add connection to group
await _signalR.AddToGroupAsync(connectionId, "admin-users");

// Remove from group
await _signalR.RemoveFromGroupAsync(connectionId, "admin-users");
```

### NotificationMessage Factory Methods

```csharp
// Info notification
var info = NotificationMessage.Info("Title", "Message", "user-id");

// Success notification
var success = NotificationMessage.Success("Order Created", "Order #123 created", "user-id");

// Warning notification
var warning = NotificationMessage.Warning("Low Stock", "Product X is low on stock", "user-id");

// Error notification
var error = NotificationMessage.Error("Payment Failed", "Card declined", "user-id");
```

## Use Cases

### 1. Real-Time Notifications
```csharp
// User registration completed
await _signalR.SendNotificationToUserAsync(
    userId, 
    NotificationMessage.Success("Welcome!", "Account created successfully.")
);
```

### 2. Admin Dashboards (Analytics API)
```csharp
// Broadcast traffic stats update every minute
await _signalR.SendMessageToGroupAsync(
    "admin-dashboard",
    "TrafficUpdate",
    new { todayViews = 1234, activeUsers = 56 }
);
```

### 3. Progress Tracking (Storage API)
```csharp
// File upload progress
for (int i = 0; i <= 100; i += 10)
{
    await _signalR.SendProgressUpdateAsync(
        userId, 
        uploadTaskId, 
        i, 
        $"Uploading file... {i}%"
    );
    await Task.Delay(500);
}
```

### 4. Group Notifications
```csharp
// Notify all users in organization
await _signalR.SendNotificationToGroupAsync(
    $"org-{organizationId}",
    NotificationMessage.Info("Maintenance", "Scheduled downtime at 2 AM")
);
```

## Redis Backplane (Production)

For multi-instance deployments, enable Redis backplane:

**appsettings.json:**
```json
{
  "SignalR": {
    "UseRedisBackplane": true,
    "RedisConnectionString": "redis:6379,password=your-password"
  }
}
```

This ensures messages sent from one instance reach clients connected to other instances.

## Security Considerations

### 1. Authentication
Use JWT tokens for authentication:

```typescript
// Client-side
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/notifications", {
    accessTokenFactory: () => localStorage.getItem("jwt")
  })
  .build();
```

### 2. Authorization
Apply `[Authorize]` attribute to hubs or methods:

```csharp
[Authorize(Roles = "Admin")]
public class AdminHub : BaseNotificationHub
{
    // Only admins can connect
}
```

### 3. CORS
Configure allowed origins in `appsettings.json`:

```json
{
  "SignalR": {
    "AllowedOrigins": "https://app.yourdomain.com,https://admin.yourdomain.com"
  }
}
```

## Integration with Existing APIs

### Analytics.Api - Real-Time Dashboard
```csharp
// Send traffic stats every minute
await _signalR.SendMessageToGroupAsync(
    "analytics-dashboard",
    "DailyStatsUpdate",
    new DailyStatsDto { Views = 1234, Clicks = 567 }
);
```

### Auth.Api - Login Notifications
```csharp
// Notify user of login from new device
await _signalR.SendNotificationToUserAsync(
    userId,
    NotificationMessage.Warning("New Login", "Login from Chrome on Windows")
);
```

### Storage.Api - Upload Progress
```csharp
// Track file upload progress
await _signalR.SendProgressUpdateAsync(userId, uploadId, 75, "Uploading...");
```

### Email.Api - Email Sent Confirmation
```csharp
// Notify user that email was sent
await _signalR.SendNotificationToUserAsync(
    userId,
    NotificationMessage.Success("Email Sent", "Password reset email sent")
);
```

## Troubleshooting

### Connection Issues
- Verify CORS is configured correctly
- Check JWT token is valid
- Ensure hub path matches client configuration
- Check firewall/network policies

### Messages Not Received
- Verify Redis connection if using backplane
- Check client event handlers are registered
- Enable detailed errors in development: `EnableDetailedErrors: true`

### Performance
- Use Redis backplane for multiple instances
- Consider message size limits (default 32KB)
- Monitor connection count with logging

## Benefits

- ✅ **Standardized**: Consistent SignalR implementation across all APIs
- ✅ **Easy Integration**: 3 lines to add to any API
- ✅ **Scalable**: Redis backplane for multi-instance deployments
- ✅ **Type-Safe**: Interface-based client contracts
- ✅ **Production-Ready**: Authentication, CORS, error handling
- ✅ **Flexible**: Custom hubs, groups, user targeting
- ✅ **Observable**: Built-in logging for debugging

## Dependencies

- Microsoft.AspNetCore.SignalR.Core
- StackExchange.Redis (for backplane)
- Microsoft.Extensions.Configuration.Binder
- Microsoft.Extensions.Logging.Abstractions

## Next Steps

1. Add `MMO.Core.SignalR` to your API project
2. Configure in `appsettings.json`
3. Register services in `Program.cs`
4. Inject `ISignalRService` into handlers
5. Configure client-side connection
6. Test real-time notifications

## Documentation

- See `SIGNALR-INTEGRATION-GUIDE.md` for step-by-step integration
- See `SIGNALR-CLIENT-EXAMPLES.md` for frontend examples
- See Analytics.Api for dashboard integration example
