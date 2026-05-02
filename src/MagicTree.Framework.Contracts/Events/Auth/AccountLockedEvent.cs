namespace MagicTree.Framework.Contracts.Events.Auth;

/// <summary>
/// Event published when user account is locked due to too many OTP failures
/// </summary>
public record AccountLockedEvent : BaseAuthEvent
{
    public DateTimeOffset LockedUntil { get; init; }

    /// <summary>
    /// Current lockout level (for escalation tracking)
    /// </summary>
    public int LockoutCount { get; init; }

    /// <summary>
    /// Duration of this lockout in seconds
    /// </summary>
    public int LockDurationSeconds { get; init; }

    /// <summary>
    /// Reason for lockout
    /// </summary>
    public string Reason { get; init; } = "TooManyOtpAttempts";
}
