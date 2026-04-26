namespace MMO.Core.CodeGenerator.Enums;

/// <summary>
/// Predefined code types with specific lengths and character sets
/// </summary>
public enum CodeType
{
    /// <summary>
    /// Numeric verification code (digits only, 6 characters)
    /// Example: 123456
    /// </summary>
    VerificationCode,

    /// <summary>
    /// Password reset token (alphanumeric, 8 characters)
    /// Example: A7B3C2D9
    /// </summary>
    PasswordResetToken,

    /// <summary>
    /// Coupon code (uppercase + digits, 10 characters)
    /// Example: SAVE20OFF5
    /// </summary>
    CouponCode,

    /// <summary>
    /// Referral code (uppercase + digits, 8 characters)
    /// Example: REF12ABC
    /// </summary>
    ReferralCode,

    /// <summary>
    /// API key (alphanumeric, 32 characters)
    /// Example: aB3dE5fG7hJ9kL2mN4pQ6rS8tU0vW
    /// </summary>
    ApiKey,

    /// <summary>
    /// Session token (alphanumeric, 64 characters)
    /// </summary>
    SessionToken,

    /// <summary>
    /// Custom code with default settings
    /// </summary>
    Custom
}
