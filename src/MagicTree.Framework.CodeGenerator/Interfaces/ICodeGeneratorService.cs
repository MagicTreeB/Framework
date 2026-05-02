namespace MagicTree.Framework.CodeGenerator.Interfaces;

/// <summary>
/// Service for generating various types of codes with configurable options
/// </summary>
public interface ICodeGeneratorService
{
    /// <summary>
    /// Generate a code with specified length
    /// </summary>
    /// <param name="length">Length of the code to generate</param>
    /// <returns>Generated code string</returns>
    string Generate(int length);

    /// <summary>
    /// Generate a code with specified length and character set options
    /// </summary>
    /// <param name="length">Length of the code</param>
    /// <param name="includeUppercase">Include uppercase letters</param>
    /// <param name="includeLowercase">Include lowercase letters</param>
    /// <param name="includeDigits">Include digits</param>
    /// <param name="includeSpecialCharacters">Include special characters</param>
    /// <param name="excludeAmbiguous">Exclude ambiguous characters (0, O, 1, I, l)</param>
    /// <returns>Generated code string</returns>
    string Generate(
        int length,
        bool includeUppercase,
        bool includeLowercase,
        bool includeDigits,
        bool includeSpecialCharacters = false,
        bool excludeAmbiguous = true);

    /// <summary>
    /// Generate a code using predefined code type
    /// </summary>
    /// <param name="codeType">Type of code to generate</param>
    /// <returns>Generated code string</returns>
    string Generate(Enums.CodeType codeType);

    /// <summary>
    /// Generate a numeric verification code (digits only)
    /// </summary>
    /// <returns>Numeric verification code</returns>
    string GenerateVerificationCode();

    /// <summary>
    /// Generate a password reset token
    /// </summary>
    /// <returns>Password reset token</returns>
    string GeneratePasswordResetToken();

    /// <summary>
    /// Generate a coupon code (uppercase + digits)
    /// </summary>
    /// <returns>Coupon code</returns>
    string GenerateCouponCode();

    /// <summary>
    /// Generate a referral code (uppercase + digits)
    /// </summary>
    /// <returns>Referral code</returns>
    string GenerateReferralCode();

    /// <summary>
    /// Generate an API key (long alphanumeric string)
    /// </summary>
    /// <returns>API key</returns>
    string GenerateApiKey();

    /// <summary>
    /// Generate a session token (very long alphanumeric string)
    /// </summary>
    /// <returns>Session token</returns>
    string GenerateSessionToken();

    /// <summary>
    /// Generate a code with custom character set
    /// </summary>
    /// <param name="length">Length of the code</param>
    /// <param name="characterSet">Custom character set to use</param>
    /// <returns>Generated code string</returns>
    string GenerateWithCustomCharacterSet(int length, string characterSet);

    /// <summary>
    /// Generate multiple unique codes
    /// </summary>
    /// <param name="count">Number of codes to generate</param>
    /// <param name="length">Length of each code</param>
    /// <returns>List of unique codes</returns>
    List<string> GenerateBatch(int count, int length);

    /// <summary>
    /// Generate multiple unique codes of specified type
    /// </summary>
    /// <param name="count">Number of codes to generate</param>
    /// <param name="codeType">Type of code to generate</param>
    /// <returns>List of unique codes</returns>
    List<string> GenerateBatch(int count, Enums.CodeType codeType);

    /// <summary>
    /// Validate if a code matches the expected format
    /// </summary>
    /// <param name="code">Code to validate</param>
    /// <param name="expectedLength">Expected length</param>
    /// <returns>True if valid, false otherwise</returns>
    bool Validate(string code, int expectedLength);
}
