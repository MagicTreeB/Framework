using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MagicTree.Framework.SignalR.Hubs;
using MagicTree.Framework.SignalR.Interfaces;
using MagicTree.Framework.SignalR.Models;

namespace MagicTree.Framework.SignalR.Services;

/// <summary>
/// Service for sending SignalR messages to clients
/// </summary>
public class SignalRService(
    IHubContext<BaseNotificationHub, INotificationHub> hubContext,
    ILogger<SignalRService> logger) : ISignalRService
{
    private readonly IHubContext<BaseNotificationHub, INotificationHub> _hubContext = hubContext;
    private readonly ILogger<SignalRService> _logger = logger;

    /// <inheritdoc/>
    public async Task SendNotificationToAllAsync(NotificationMessage notification, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.All.ReceiveNotification(notification);
            _logger.LogInformation("Notification sent to all clients: {Title}", notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to all clients: {Title}", notification.Title);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SendNotificationToUserAsync(string userId, NotificationMessage notification, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.User(userId).ReceiveNotification(notification);
            _logger.LogInformation("Notification sent to user {UserId}: {Title}", userId, notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}: {Title}", userId, notification.Title);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SendNotificationToGroupAsync(string groupName, NotificationMessage notification, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.Group(groupName).ReceiveNotification(notification);
            _logger.LogInformation("Notification sent to group {GroupName}: {Title}", groupName, notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to group {GroupName}: {Title}", groupName, notification.Title);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SendNotificationToUsersAsync(IEnumerable<string> userIds, NotificationMessage notification, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.Users(userIds).ReceiveNotification(notification);
            _logger.LogInformation("Notification sent to {UserCount} users: {Title}", userIds.Count(), notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to multiple users: {Title}", notification.Title);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SendMessageToAllAsync(string method, object message, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.All.ReceiveMessage(method, message);
            _logger.LogInformation("Message sent to all clients via method: {Method}", method);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to all clients via method: {Method}", method);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SendMessageToUserAsync(string userId, string method, object message, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.User(userId).ReceiveMessage(method, message);
            _logger.LogInformation("Message sent to user {UserId} via method: {Method}", userId, method);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to user {UserId} via method: {Method}", userId, method);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SendMessageToGroupAsync(string groupName, string method, object message, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.Group(groupName).ReceiveMessage(method, message);
            _logger.LogInformation("Message sent to group {GroupName} via method: {Method}", groupName, method);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to group {GroupName} via method: {Method}", groupName, method);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SendProgressUpdateAsync(string userId, string taskId, int progress, string message, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.User(userId).ProgressUpdate(taskId, progress, message);
            _logger.LogDebug("Progress update sent to user {UserId}: Task {TaskId} - {Progress}%", userId, taskId, progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send progress update to user {UserId}: Task {TaskId}", userId, taskId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task AddToGroupAsync(string connectionId, string groupName, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Groups.AddToGroupAsync(connectionId, groupName, ct);
            _logger.LogInformation("Connection {ConnectionId} added to group {GroupName}", connectionId, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add connection {ConnectionId} to group {GroupName}", connectionId, groupName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName, ct);
            _logger.LogInformation("Connection {ConnectionId} removed from group {GroupName}", connectionId, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove connection {ConnectionId} from group {GroupName}", connectionId, groupName);
            throw;
        }
    }
}
