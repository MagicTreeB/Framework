using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MagicTree.Framework.SignalR.Interfaces;

namespace MagicTree.Framework.SignalR.Hubs;

/// <summary>
/// Base hub for real-time notifications with authentication support
/// </summary>
public class BaseNotificationHub(ILogger<BaseNotificationHub> logger) : Hub<INotificationHub>
{
    private readonly ILogger<BaseNotificationHub> _logger = logger;

    /// <summary>
    /// Called when a new connection is established
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var userId = Context.User?.Identity?.Name;

        _logger.LogInformation("Client connected - ConnectionId: {ConnectionId}, UserId: {UserId}", 
            connectionId, userId ?? "Anonymous");

        // Notify client of successful connection
        await Clients.Caller.ConnectionUpdate(connectionId, "Connected");

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a connection is disconnected
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        var userId = Context.User?.Identity?.Name;

        if (exception != null)
        {
            _logger.LogWarning(exception, "Client disconnected with error - ConnectionId: {ConnectionId}, UserId: {UserId}", 
                connectionId, userId ?? "Anonymous");
        }
        else
        {
            _logger.LogInformation("Client disconnected - ConnectionId: {ConnectionId}, UserId: {UserId}", 
                connectionId, userId ?? "Anonymous");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a specific group
    /// </summary>
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("User {UserId} joined group {GroupName}", 
            Context.User?.Identity?.Name ?? "Anonymous", groupName);
    }

    /// <summary>
    /// Leave a specific group
    /// </summary>
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("User {UserId} left group {GroupName}", 
            Context.User?.Identity?.Name ?? "Anonymous", groupName);
    }

    /// <summary>
    /// Get current connection ID
    /// </summary>
    public string GetConnectionId() => Context.ConnectionId;

    /// <summary>
    /// Get current user ID
    /// </summary>
    public string? GetUserId() => Context.User?.Identity?.Name;
}
