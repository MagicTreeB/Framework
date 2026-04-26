
namespace MMO.Core.Contracts.Events.Auth;

public record AdminResetPasswordEvent: BaseAuthEvent
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