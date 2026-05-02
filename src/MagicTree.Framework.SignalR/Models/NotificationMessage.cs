namespace MagicTree.Framework.SignalR.Models;

/// <summary>
/// Standard notification message model
/// </summary>
public class NotificationMessage
{
    /// <summary>
    /// Notification ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Notification type (Info, Success, Warning, Error)
    /// </summary>
    public string Type { get; set; } = "Info";

    /// <summary>
    /// Notification title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notification message content
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when notification was created
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional data payload
    /// </summary>
    public Dictionary<string, object>? Data { get; set; }

    /// <summary>
    /// Target user ID (null for broadcast)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Target group name (null for user-specific)
    /// </summary>
    public string? GroupName { get; set; }

    /// <summary>
    /// Create an Info notification
    /// </summary>
    public static NotificationMessage Info(string title, string message, string? userId = null) =>
        new() { Type = "Info", Title = title, Message = message, UserId = userId };

    /// <summary>
    /// Create a Success notification
    /// </summary>
    public static NotificationMessage Success(string title, string message, string? userId = null) =>
        new() { Type = "Success", Title = title, Message = message, UserId = userId };

    /// <summary>
    /// Create a Warning notification
    /// </summary>
    public static NotificationMessage Warning(string title, string message, string? userId = null) =>
        new() { Type = "Warning", Title = title, Message = message, UserId = userId };

    /// <summary>
    /// Create an Error notification
    /// </summary>
    public static NotificationMessage Error(string title, string message, string? userId = null) =>
        new() { Type = "Error", Title = title, Message = message, UserId = userId };
}
