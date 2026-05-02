namespace MagicTree.Framework.CodeGenerator.Options;

/// <summary>
/// Configuration options for code generation
/// </summary>
public class CodeGeneratorOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public static string SectionName => "CodeGenerator";

    /// <summary>
    /// Default code length if not specified. Default: 6
    /// </summary>
    public int DefaultLength { get; set; } = 6;

    /// <summary>
    /// Verification code length (email, SMS). Default: 6
    /// </summary>
    public int VerificationCodeLength { get; set; } = 6;

    /// <summary>
    /// Password reset token length. Default: 8
    /// </summary>
    public int PasswordResetTokenLength { get; set; } = 8;

    /// <summary>
    /// Coupon code length. Default: 10
    /// </summary>
    public int CouponCodeLength { get; set; } = 10;

    /// <summary>
    /// Referral code length. Default: 8
    /// </summary>
    public int ReferralCodeLength { get; set; } = 8;

    /// <summary>
    /// API key length. Default: 32
    /// </summary>
    public int ApiKeyLength { get; set; } = 32;

    /// <summary>
    /// Session token length. Default: 64
    /// </summary>
    public int SessionTokenLength { get; set; } = 64;

    /// <summary>
    /// Include uppercase letters (A-Z). Default: true
    /// </summary>
    public bool IncludeUppercase { get; set; } = true;

    /// <summary>
    /// Include lowercase letters (a-z). Default: true
    /// </summary>
    public bool IncludeLowercase { get; set; } = true;

    /// <summary>
    /// Include digits (0-9). Default: true
    /// </summary>
    public bool IncludeDigits { get; set; } = true;

    /// <summary>
    /// Include special characters (!@#$%^&*). Default: false
    /// </summary>
    public bool IncludeSpecialCharacters { get; set; } = false;

    /// <summary>
    /// Exclude ambiguous characters (0, O, 1, I, l). Default: true
    /// </summary>
    public bool ExcludeAmbiguous { get; set; } = true;

    /// <summary>
    /// Custom character set to use (overrides other options if set)
    /// </summary>
    public string? CustomCharacterSet { get; set; }
}
