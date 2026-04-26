using MMO.Core.CodeGenerator.Options;

namespace MMO.Core.CodeGenerator.UnitTest.Options;

public class CodeGeneratorOptionsTests
{
    [Fact]
    public void CodeGeneratorOptions_ShouldHaveCorrectSectionName()
    {
        // Assert
        CodeGeneratorOptions.SectionName.Should().Be("CodeGenerator");
    }

    [Fact]
    public void CodeGeneratorOptions_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var options = new CodeGeneratorOptions();

        // Assert
        options.DefaultLength.Should().Be(6);
        options.VerificationCodeLength.Should().Be(6);
        options.PasswordResetTokenLength.Should().Be(8);
        options.CouponCodeLength.Should().Be(10);
        options.ReferralCodeLength.Should().Be(8);
        options.ApiKeyLength.Should().Be(32);
        options.SessionTokenLength.Should().Be(64);
    }

    [Fact]
    public void CodeGeneratorOptions_CanSetCustomValues()
    {
        // Arrange
        var options = new CodeGeneratorOptions
        {
            DefaultLength = 12,
            VerificationCodeLength = 4,
            PasswordResetTokenLength = 16,
            CouponCodeLength = 15,
            ReferralCodeLength = 10,
            ApiKeyLength = 64,
            SessionTokenLength = 128
        };

        // Assert
        options.DefaultLength.Should().Be(12);
        options.VerificationCodeLength.Should().Be(4);
        options.PasswordResetTokenLength.Should().Be(16);
        options.CouponCodeLength.Should().Be(15);
        options.ReferralCodeLength.Should().Be(10);
        options.ApiKeyLength.Should().Be(64);
        options.SessionTokenLength.Should().Be(128);
    }

    [Fact]
    public void CodeGeneratorOptions_AllProperties_ShouldBeSettable()
    {
        // Arrange
        var options = new CodeGeneratorOptions();

        // Act
        options.DefaultLength = 20;
        options.VerificationCodeLength = 8;
        options.PasswordResetTokenLength = 12;
        options.CouponCodeLength = 18;
        options.ReferralCodeLength = 14;
        options.ApiKeyLength = 40;
        options.SessionTokenLength = 100;

        // Assert
        options.DefaultLength.Should().Be(20);
        options.VerificationCodeLength.Should().Be(8);
        options.PasswordResetTokenLength.Should().Be(12);
        options.CouponCodeLength.Should().Be(18);
        options.ReferralCodeLength.Should().Be(14);
        options.ApiKeyLength.Should().Be(40);
        options.SessionTokenLength.Should().Be(100);
    }
}
