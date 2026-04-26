using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MMO.Core.CodeGenerator.Enums;
using MMO.Core.CodeGenerator.Interfaces;
using MMO.Core.CodeGenerator.Options;

namespace MMO.Core.CodeGenerator.Services;

/// <summary>
/// Service implementation for generating cryptographically secure codes
/// </summary>
public class CodeGeneratorService : ICodeGeneratorService
{
    private readonly CodeGeneratorOptions _options;

    // Character sets
    private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    private const string Digits = "0123456789";
    private const string SpecialCharacters = "!@#$%^&*";
    private const string AmbiguousCharacters = "0O1Il";

    public CodeGeneratorService(IOptions<CodeGeneratorOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public string Generate(int length)
    {
        return Generate(
            length,
            _options.IncludeUppercase,
            _options.IncludeLowercase,
            _options.IncludeDigits,
            _options.IncludeSpecialCharacters,
            _options.ExcludeAmbiguous);
    }

    /// <inheritdoc />
    public string Generate(
        int length,
        bool includeUppercase,
        bool includeLowercase,
        bool includeDigits,
        bool includeSpecialCharacters = false,
        bool excludeAmbiguous = true)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be greater than 0", nameof(length));

        var characterSet = BuildCharacterSet(
            includeUppercase,
            includeLowercase,
            includeDigits,
            includeSpecialCharacters,
            excludeAmbiguous);

        if (string.IsNullOrEmpty(characterSet))
            throw new InvalidOperationException("No valid character set defined. Enable at least one character type.");

        return GenerateSecureRandom(length, characterSet);
    }

    /// <inheritdoc />
    public string Generate(CodeType codeType)
    {
        return codeType switch
        {
            CodeType.VerificationCode => GenerateVerificationCode(),
            CodeType.PasswordResetToken => GeneratePasswordResetToken(),
            CodeType.CouponCode => GenerateCouponCode(),
            CodeType.ReferralCode => GenerateReferralCode(),
            CodeType.ApiKey => GenerateApiKey(),
            CodeType.SessionToken => GenerateSessionToken(),
            CodeType.Custom => Generate(_options.DefaultLength),
            _ => throw new ArgumentException($"Unknown code type: {codeType}", nameof(codeType))
        };
    }

    /// <inheritdoc />
    public string GenerateVerificationCode()
    {
        // Digits only, no ambiguous characters
        return GenerateSecureRandom(_options.VerificationCodeLength, Digits);
    }

    /// <inheritdoc />
    public string GeneratePasswordResetToken()
    {
        // Uppercase + digits, no ambiguous
        var characterSet = BuildCharacterSet(true, false, true, false, true);
        return GenerateSecureRandom(_options.PasswordResetTokenLength, characterSet);
    }

    /// <inheritdoc />
    public string GenerateCouponCode()
    {
        // Uppercase + digits, no ambiguous
        var characterSet = BuildCharacterSet(true, false, true, false, true);
        return GenerateSecureRandom(_options.CouponCodeLength, characterSet);
    }

    /// <inheritdoc />
    public string GenerateReferralCode()
    {
        // Uppercase + digits, no ambiguous
        var characterSet = BuildCharacterSet(true, false, true, false, true);
        return GenerateSecureRandom(_options.ReferralCodeLength, characterSet);
    }

    /// <inheritdoc />
    public string GenerateApiKey()
    {
        // Uppercase + lowercase + digits, no special chars, no ambiguous
        var characterSet = BuildCharacterSet(true, true, true, false, true);
        return GenerateSecureRandom(_options.ApiKeyLength, characterSet);
    }

    /// <inheritdoc />
    public string GenerateSessionToken()
    {
        // Uppercase + lowercase + digits, no special chars, no ambiguous
        var characterSet = BuildCharacterSet(true, true, true, false, true);
        return GenerateSecureRandom(_options.SessionTokenLength, characterSet);
    }

    /// <inheritdoc />
    public string GenerateWithCustomCharacterSet(int length, string characterSet)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be greater than 0", nameof(length));

        if (string.IsNullOrEmpty(characterSet))
            throw new ArgumentException("Character set cannot be empty", nameof(characterSet));

        return GenerateSecureRandom(length, characterSet);
    }

    /// <inheritdoc />
    public List<string> GenerateBatch(int count, int length)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be greater than 0", nameof(count));

        var codes = new HashSet<string>();
        var maxAttempts = count * 10; // Prevent infinite loop
        var attempts = 0;

        while (codes.Count < count && attempts < maxAttempts)
        {
            codes.Add(Generate(length));
            attempts++;
        }

        if (codes.Count < count)
            throw new InvalidOperationException($"Could not generate {count} unique codes. Consider increasing code length.");

        return codes.ToList();
    }

    /// <inheritdoc />
    public List<string> GenerateBatch(int count, CodeType codeType)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be greater than 0", nameof(count));

        var codes = new HashSet<string>();
        var maxAttempts = count * 10;
        var attempts = 0;

        while (codes.Count < count && attempts < maxAttempts)
        {
            codes.Add(Generate(codeType));
            attempts++;
        }

        if (codes.Count < count)
            throw new InvalidOperationException($"Could not generate {count} unique codes of type {codeType}.");

        return codes.ToList();
    }

    /// <inheritdoc />
    public bool Validate(string code, int expectedLength)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        return code.Length == expectedLength;
    }

    #region Private Methods

    private string BuildCharacterSet(
        bool includeUppercase,
        bool includeLowercase,
        bool includeDigits,
        bool includeSpecialCharacters,
        bool excludeAmbiguous)
    {
        // Use custom character set if provided
        if (!string.IsNullOrEmpty(_options.CustomCharacterSet))
            return _options.CustomCharacterSet;

        var sb = new StringBuilder();

        if (includeUppercase)
            sb.Append(Uppercase);

        if (includeLowercase)
            sb.Append(Lowercase);

        if (includeDigits)
            sb.Append(Digits);

        if (includeSpecialCharacters)
            sb.Append(SpecialCharacters);

        var characterSet = sb.ToString();

        // Remove ambiguous characters if requested
        if (excludeAmbiguous && !string.IsNullOrEmpty(characterSet))
        {
            characterSet = new string(characterSet
                .Where(c => !AmbiguousCharacters.Contains(c))
                .ToArray());
        }

        return characterSet;
    }

    private static string GenerateSecureRandom(int length, string characterSet)
    {
        var result = new char[length];
        var characterSetLength = characterSet.Length;

        // Use cryptographically secure random number generator
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[length * 4]; // 4 bytes per character for better distribution
        rng.GetBytes(randomBytes);

        for (int i = 0; i < length; i++)
        {
            // Convert 4 bytes to uint for better distribution
            var randomValue = BitConverter.ToUInt32(randomBytes, i * 4);
            var index = (int)(randomValue % (uint)characterSetLength);
            result[i] = characterSet[index];
        }

        return new string(result);
    }

    #endregion
}
