namespace MagicTree.Framework.Contracts.Events.Auth;

/// <summary>
/// Event published when two-factor authentication is enabled or disabled
/// </summary>
public record TwoFactorStatusChangedEvent : BaseAuthEvent
{
    /// <summary>
    /// Whether 2FA is now enabled (true) or disabled (false)
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Timestamp when 2FA status was changed
    /// </summary>
    public DateTimeOffset ChangedAt { get; init; }

    /// <summary>
    /// IP address from which the change was made
    /// </summary>
    public string IpAddress { get; init; } = string.Empty;

    /// <summary>
    /// User agent of the device that made the change
    /// </summary>
    public string? UserAgent { get; init; }
}
