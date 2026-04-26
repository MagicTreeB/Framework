namespace MMO.Core.Contracts.Events.Auth;

/// <summary>
/// Event published when a user requests password reset via OTP
/// </summary>
public record ForgotPasswordEvent : BaseAuthEvent
{
    /// <summary>
    /// Url to reset password
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// OTP expiration timestamp
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }
}
