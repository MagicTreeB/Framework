namespace MagicTree.Framework.Contracts.Events.Auth;

/// <summary>
/// Base class for all authentication-related events
/// </summary>
public abstract record BaseAuthEvent
{
    /// <summary>
    /// Unique identifier for message deduplication
    /// </summary>
    public Guid MessageId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// User ID associated with the event
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// User's first name for email personalization
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// User's last name for email personalization
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Timestamp when the event was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
