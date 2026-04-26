namespace MMO.Core.Contracts.Events.Auth;

/// <summary>
/// Event published when a user sets up two-factor authentication
/// </summary>
public record TwoFactorSetupEvent : BaseAuthEvent
{
    /// <summary>
    /// QR code URL for authenticator app setup
    /// </summary>
    public string QrCodeUrl { get; init; } = string.Empty;

    /// <summary>
    /// Manual entry key for authenticator apps that don't support QR codes
    /// </summary>
    public string ManualEntryKey { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when 2FA was set up
    /// </summary>
    public DateTimeOffset SetupAt { get; init; }
}
