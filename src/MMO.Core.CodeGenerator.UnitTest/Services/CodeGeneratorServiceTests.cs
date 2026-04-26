using Microsoft.Extensions.Options;
using MMO.Core.CodeGenerator.Enums;
using MMO.Core.CodeGenerator.Options;
using MMO.Core.CodeGenerator.Services;

namespace MMO.Core.CodeGenerator.UnitTest.Services;

public class CodeGeneratorServiceTests
{
    private readonly CodeGeneratorOptions _defaultOptions;
    private readonly CodeGeneratorService _service;

    public CodeGeneratorServiceTests()
    {
        _defaultOptions = new CodeGeneratorOptions();
        _service = new CodeGeneratorService(Microsoft.Extensions.Options.Options.Create(_defaultOptions));
    }

    #region Generate(int length) Tests

    [Fact]
    public void Generate_WithValidLength_ShouldReturnCodeOfSpecifiedLength()
    {
        // Arrange
        var length = 10;

        // Act
        var code = _service.Generate(length);

        // Assert
        code.Should().NotBeNullOrEmpty();
        code.Length.Should().Be(length);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(32)]
    [InlineData(64)]
    public void Generate_WithVariousLengths_ShouldReturnCorrectLength(int length)
    {
        // Act
        var code = _service.Generate(length);

        // Assert
        code.Length.Should().Be(length);
    }

    [Fact]
    public void Generate_WithZeroLength_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _service.Generate(0);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Length must be greater than 0*");
    }

    [Fact]
    public void Generate_WithNegativeLength_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _service.Generate(-5);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Length must be greater than 0*");
    }

    [Fact]
    public void Generate_MultipleTimes_ShouldGenerateDifferentCodes()
    {
        // Act
        var code1 = _service.Generate(10);
        var code2 = _service.Generate(10);
        var code3 = _service.Generate(10);

        // Assert
        code1.Should().NotBe(code2);
        code2.Should().NotBe(code3);
        code1.Should().NotBe(code3);
    }

    #endregion

    #region Generate with Character Set Options Tests

    [Fact]
    public void Generate_WithUppercaseOnly_ShouldContainOnlyUppercase()
    {
        // Act
        var code = _service.Generate(20, includeUppercase: true, includeLowercase: false, includeDigits: false);

        // Assert
        code.Should().MatchRegex("^[A-Z]+$");
    }

    [Fact]
    public void Generate_WithLowercaseOnly_ShouldContainOnlyLowercase()
    {
        // Act
        var code = _service.Generate(20, includeUppercase: false, includeLowercase: true, includeDigits: false);

        // Assert
        code.Should().MatchRegex("^[a-z]+$");
    }

    [Fact]
    public void Generate_WithDigitsOnly_ShouldContainOnlyDigits()
    {
        // Act
        var code = _service.Generate(20, includeUppercase: false, includeLowercase: false, includeDigits: true);

        // Assert
        code.Should().MatchRegex("^[0-9]+$");
    }

    [Fact]
    public void Generate_WithAllCharacterTypes_ShouldContainMixedCharacters()
    {
        // Act
        var code = _service.Generate(50, includeUppercase: true, includeLowercase: true, includeDigits: true, includeSpecialCharacters: true, excludeAmbiguous: false);

        // Assert - Should contain at least one of each type (statistically likely with 50 chars)
        code.Should().MatchRegex("[A-Z]"); // Has uppercase
        code.Should().MatchRegex("[a-z]"); // Has lowercase
        code.Should().MatchRegex("[0-9]"); // Has digits
    }

    [Fact]
    public void Generate_WithNoCharacterTypesEnabled_ShouldThrowInvalidOperationException()
    {
        // Act
        var act = () => _service.Generate(10, includeUppercase: false, includeLowercase: false, includeDigits: false);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No valid character set defined*");
    }

    [Fact]
    public void Generate_WithExcludeAmbiguous_ShouldNotContainAmbiguousCharacters()
    {
        // Act - Generate many codes to ensure no ambiguous chars appear
        var codes = Enumerable.Range(0, 100).Select(_ => _service.Generate(20, true, true, true, false, excludeAmbiguous: true)).ToList();

        // Assert - None should contain 0, O, 1, I, l
        foreach (var code in codes)
        {
            code.Should().NotContain("0");
            code.Should().NotContain("O");
            code.Should().NotContain("1");
            code.Should().NotContain("I");
            code.Should().NotContain("l");
        }
    }

    #endregion

    #region Generate(CodeType) Tests

    [Fact]
    public void Generate_VerificationCodeType_ShouldReturnDigitsOnly()
    {
        // Act
        var code = _service.Generate(CodeType.VerificationCode);

        // Assert
        code.Should().MatchRegex("^[0-9]+$");
        code.Length.Should().Be(_defaultOptions.VerificationCodeLength);
    }

    [Fact]
    public void Generate_PasswordResetTokenType_ShouldReturnUppercaseAndDigits()
    {
        // Act
        var code = _service.Generate(CodeType.PasswordResetToken);

        // Assert
        code.Should().MatchRegex("^[A-Z0-9]+$");
        code.Length.Should().Be(_defaultOptions.PasswordResetTokenLength);
    }

    [Fact]
    public void Generate_CouponCodeType_ShouldReturnCorrectFormat()
    {
        // Act
        var code = _service.Generate(CodeType.CouponCode);

        // Assert
        code.Should().MatchRegex("^[A-Z0-9]+$");
        code.Length.Should().Be(_defaultOptions.CouponCodeLength);
    }

    [Fact]
    public void Generate_ReferralCodeType_ShouldReturnCorrectFormat()
    {
        // Act
        var code = _service.Generate(CodeType.ReferralCode);

        // Assert
        code.Should().MatchRegex("^[A-Z0-9]+$");
        code.Length.Should().Be(_defaultOptions.ReferralCodeLength);
    }

    [Fact]
    public void Generate_ApiKeyType_ShouldReturnLongAlphanumeric()
    {
        // Act
        var code = _service.Generate(CodeType.ApiKey);

        // Assert
        code.Should().MatchRegex("^[A-Za-z0-9]+$");
        code.Length.Should().Be(_defaultOptions.ApiKeyLength);
    }

    [Fact]
    public void Generate_SessionTokenType_ShouldReturnVeryLongAlphanumeric()
    {
        // Act
        var code = _service.Generate(CodeType.SessionToken);

        // Assert
        code.Should().MatchRegex("^[A-Za-z0-9]+$");
        code.Length.Should().Be(_defaultOptions.SessionTokenLength);
    }

    [Fact]
    public void Generate_CustomType_ShouldUseDefaultLength()
    {
        // Act
        var code = _service.Generate(CodeType.Custom);

        // Assert
        code.Length.Should().Be(_defaultOptions.DefaultLength);
    }

    #endregion

    #region Specific Generator Methods Tests

    [Fact]
    public void GenerateVerificationCode_ShouldReturnNumericCode()
    {
        // Act
        var code = _service.GenerateVerificationCode();

        // Assert
        code.Should().MatchRegex("^[0-9]+$");
        code.Length.Should().Be(_defaultOptions.VerificationCodeLength);
    }

    [Fact]
    public void GeneratePasswordResetToken_ShouldReturnAlphanumericToken()
    {
        // Act
        var code = _service.GeneratePasswordResetToken();

        // Assert
        code.Should().MatchRegex("^[A-Z0-9]+$");
        code.Length.Should().Be(_defaultOptions.PasswordResetTokenLength);
    }

    [Fact]
    public void GenerateCouponCode_ShouldReturnCouponFormat()
    {
        // Act
        var code = _service.GenerateCouponCode();

        // Assert
        code.Should().MatchRegex("^[A-Z0-9]+$");
        code.Length.Should().Be(_defaultOptions.CouponCodeLength);
    }

    [Fact]
    public void GenerateReferralCode_ShouldReturnReferralFormat()
    {
        // Act
        var code = _service.GenerateReferralCode();

        // Assert
        code.Should().MatchRegex("^[A-Z0-9]+$");
        code.Length.Should().Be(_defaultOptions.ReferralCodeLength);
    }

    [Fact]
    public void GenerateApiKey_ShouldReturnLongKey()
    {
        // Act
        var code = _service.GenerateApiKey();

        // Assert
        code.Should().MatchRegex("^[A-Za-z0-9]+$");
        code.Length.Should().Be(_defaultOptions.ApiKeyLength);
    }

    [Fact]
    public void GenerateSessionToken_ShouldReturnVeryLongToken()
    {
        // Act
        var code = _service.GenerateSessionToken();

        // Assert
        code.Should().MatchRegex("^[A-Za-z0-9]+$");
        code.Length.Should().Be(_defaultOptions.SessionTokenLength);
    }

    #endregion

    #region GenerateWithCustomCharacterSet Tests

    [Fact]
    public void GenerateWithCustomCharacterSet_WithValidInputs_ShouldReturnCodeFromCharacterSet()
    {
        // Arrange
        var characterSet = "ABC123";

        // Act
        var code = _service.GenerateWithCustomCharacterSet(10, characterSet);

        // Assert
        code.Length.Should().Be(10);
        code.Should().MatchRegex("^[ABC123]+$");
    }

    [Fact]
    public void GenerateWithCustomCharacterSet_WithZeroLength_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _service.GenerateWithCustomCharacterSet(0, "ABC");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Length must be greater than 0*");
    }

    [Fact]
    public void GenerateWithCustomCharacterSet_WithEmptyCharacterSet_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _service.GenerateWithCustomCharacterSet(10, "");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Character set cannot be empty*");
    }

    [Fact]
    public void GenerateWithCustomCharacterSet_WithNullCharacterSet_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _service.GenerateWithCustomCharacterSet(10, null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Character set cannot be empty*");
    }

    #endregion

    #region GenerateBatch Tests

    [Fact]
    public void GenerateBatch_WithValidCount_ShouldReturnSpecifiedNumberOfCodes()
    {
        // Arrange
        var count = 10;
        var length = 8;

        // Act
        var codes = _service.GenerateBatch(count, length);

        // Assert
        codes.Should().HaveCount(count);
        codes.Should().OnlyHaveUniqueItems();
        codes.Should().AllSatisfy(code => code.Length.Should().Be(length));
    }

    [Fact]
    public void GenerateBatch_ShouldGenerateUniqueCodes()
    {
        // Act
        var codes = _service.GenerateBatch(50, 12);

        // Assert
        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GenerateBatch_WithZeroCount_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _service.GenerateBatch(0, 8);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Count must be greater than 0*");
    }

    [Fact]
    public void GenerateBatch_WithNegativeCount_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _service.GenerateBatch(-5, 8);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Count must be greater than 0*");
    }

    [Fact]
    public void GenerateBatch_WithCodeType_ShouldReturnCodesOfSpecifiedType()
    {
        // Act
        var codes = _service.GenerateBatch(10, CodeType.CouponCode);

        // Assert
        codes.Should().HaveCount(10);
        codes.Should().OnlyHaveUniqueItems();
        codes.Should().AllSatisfy(code =>
        {
            code.Should().MatchRegex("^[A-Z0-9]+$");
            code.Length.Should().Be(_defaultOptions.CouponCodeLength);
        });
    }

    [Fact]
    public void GenerateBatch_WithVerificationCodeType_ShouldReturnNumericCodes()
    {
        // Act
        var codes = _service.GenerateBatch(5, CodeType.VerificationCode);

        // Assert
        codes.Should().HaveCount(5);
        codes.Should().OnlyHaveUniqueItems();
        codes.Should().AllSatisfy(code => code.Should().MatchRegex("^[0-9]+$"));
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void Validate_WithValidCode_ShouldReturnTrue()
    {
        // Arrange
        var code = "ABC123";
        var expectedLength = 6;

        // Act
        var result = _service.Validate(code, expectedLength);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithIncorrectLength_ShouldReturnFalse()
    {
        // Arrange
        var code = "ABC123";
        var expectedLength = 8;

        // Act
        var result = _service.Validate(code, expectedLength);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyCode_ShouldReturnFalse()
    {
        // Act
        var result = _service.Validate("", 6);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithNullCode_ShouldReturnFalse()
    {
        // Act
        var result = _service.Validate(null!, 6);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithWhitespaceCode_ShouldReturnFalse()
    {
        // Act
        var result = _service.Validate("   ", 3);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Custom Options Tests

    [Fact]
    public void CodeGeneratorService_WithCustomOptions_ShouldUseCustomLengths()
    {
        // Arrange
        var customOptions = new CodeGeneratorOptions
        {
            VerificationCodeLength = 4,
            CouponCodeLength = 12
        };
        var customService = new CodeGeneratorService(Microsoft.Extensions.Options.Options.Create(customOptions));

        // Act
        var verificationCode = customService.GenerateVerificationCode();
        var couponCode = customService.GenerateCouponCode();

        // Assert
        verificationCode.Length.Should().Be(4);
        couponCode.Length.Should().Be(12);
    }

    #endregion

    #region Cryptographic Security Tests

    [Fact]
    public void Generate_MultipleCalls_ShouldHaveHighEntropy()
    {
        // Act - Generate 1000 codes
        var codes = Enumerable.Range(0, 1000)
            .Select(_ => _service.Generate(10))
            .ToList();

        // Assert - All should be unique (high entropy)
        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Generate_ShouldNotProducePredictablePatterns()
    {
        // Act
        var codes = Enumerable.Range(0, 100)
            .Select(_ => _service.GenerateVerificationCode())
            .ToList();

        // Assert - Should not have sequential patterns like "123456", "111111", etc.
        codes.Should().NotContain("123456");
        codes.Should().NotContain("654321");
        codes.Should().NotContain("000000");
        codes.Should().NotContain("111111");
        codes.Should().NotContain("999999");
    }

    #endregion
}
