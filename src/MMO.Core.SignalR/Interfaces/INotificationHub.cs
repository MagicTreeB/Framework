using MMO.Core.SignalR.Models;

namespace MMO.Core.SignalR.Interfaces;

/// <summary>
/// Client-side methods that can be invoked from the server
/// </summary>
public interface INotificationHub
{
    /// <summary>
    /// Receive a notification message
    /// </summary>
    Task ReceiveNotification(NotificationMessage notification);

    /// <summary>
    /// Receive a typed message
    /// </summary>
    Task ReceiveMessage(string method, object message);

    /// <summary>
    /// Receive connection update
    /// </summary>
    Task ConnectionUpdate(string connectionId, string status);

    /// <summary>
    /// Receive user count update
    /// </summary>
    Task UserCountUpdate(int count);

    /// <summary>
    /// Receive progress update
    /// </summary>
    Task ProgressUpdate(string taskId, int progress, string message);
}
