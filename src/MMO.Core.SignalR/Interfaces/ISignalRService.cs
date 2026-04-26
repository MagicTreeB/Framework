using MMO.Core.SignalR.Models;

namespace MMO.Core.SignalR.Interfaces;

/// <summary>
/// Service for sending SignalR messages
/// </summary>
public interface ISignalRService
{
    /// <summary>
    /// Send notification to all connected clients
    /// </summary>
    Task SendNotificationToAllAsync(NotificationMessage notification, CancellationToken ct = default);

    /// <summary>
    /// Send notification to specific user
    /// </summary>
    Task SendNotificationToUserAsync(string userId, NotificationMessage notification, CancellationToken ct = default);

    /// <summary>
    /// Send notification to specific group
    /// </summary>
    Task SendNotificationToGroupAsync(string groupName, NotificationMessage notification, CancellationToken ct = default);

    /// <summary>
    /// Send notification to multiple users
    /// </summary>
    Task SendNotificationToUsersAsync(IEnumerable<string> userIds, NotificationMessage notification, CancellationToken ct = default);

    /// <summary>
    /// Send message to all connected clients
    /// </summary>
    Task SendMessageToAllAsync(string method, object message, CancellationToken ct = default);

    /// <summary>
    /// Send message to specific user
    /// </summary>
    Task SendMessageToUserAsync(string userId, string method, object message, CancellationToken ct = default);

    /// <summary>
    /// Send message to specific group
    /// </summary>
    Task SendMessageToGroupAsync(string groupName, string method, object message, CancellationToken ct = default);

    /// <summary>
    /// Send progress update
    /// </summary>
    Task SendProgressUpdateAsync(string userId, string taskId, int progress, string message, CancellationToken ct = default);

    /// <summary>
    /// Add user to group
    /// </summary>
    Task AddToGroupAsync(string connectionId, string groupName, CancellationToken ct = default);

    /// <summary>
    /// Remove user from group
    /// </summary>
    Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken ct = default);
}
