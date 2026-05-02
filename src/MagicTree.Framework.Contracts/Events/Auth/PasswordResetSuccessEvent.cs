namespace MagicTree.Framework.Contracts.Events.Auth;

/// <summary>
/// Event published when password reset is successfully completed
/// </summary>
public record PasswordResetSuccessEvent : BaseAuthEvent
{
    /// <summary>
    /// Timestamp when password was reset
    /// </summary>
    public DateTimeOffset ResetAt { get; init; }
}
