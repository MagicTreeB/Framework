using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MagicTree.Framework.CodeGenerator.Extensions;
using MagicTree.Framework.CodeGenerator.Interfaces;
using MagicTree.Framework.CodeGenerator.Options;

namespace MagicTree.Framework.CodeGenerator.UnitTest.Extensions;

public class CodeGeneratorExtensionsTests
{
    [Fact]
    public void AddCodeGenerator_WithConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CodeGenerator:DefaultLength"] = "8",
                ["CodeGenerator:VerificationCodeLength"] = "6"
            })
            .Build();

        // Act
        services.AddCodeGenerator(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var codeGeneratorService = serviceProvider.GetService<ICodeGeneratorService>();
        codeGeneratorService.Should().NotBeNull();
    }

    [Fact]
    public void AddCodeGenerator_WithConfiguration_ShouldBindOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CodeGenerator:DefaultLength"] = "10",
                ["CodeGenerator:VerificationCodeLength"] = "8",
                ["CodeGenerator:CouponCodeLength"] = "12"
            })
            .Build();

        // Act
        services.AddCodeGenerator(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<CodeGeneratorOptions>>();

        // Assert
        options.Should().NotBeNull();
        options!.Value.DefaultLength.Should().Be(10);
        options.Value.VerificationCodeLength.Should().Be(8);
        options.Value.CouponCodeLength.Should().Be(12);
    }

    [Fact]
    public void AddCodeGenerator_WithCustomOptions_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCodeGenerator(options =>
        {
            options.DefaultLength = 12;
            options.VerificationCodeLength = 4;
            options.ApiKeyLength = 48;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var codeGeneratorService = serviceProvider.GetService<ICodeGeneratorService>();
        codeGeneratorService.Should().NotBeNull();
    }

    [Fact]
    public void AddCodeGenerator_WithCustomOptions_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCodeGenerator(options =>
        {
            options.DefaultLength = 15;
            options.CouponCodeLength = 20;
        });
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<CodeGeneratorOptions>>();

        // Assert
        options.Should().NotBeNull();
        options!.Value.DefaultLength.Should().Be(15);
        options.Value.CouponCodeLength.Should().Be(20);
    }

    [Fact]
    public void AddCodeGenerator_ShouldReturnSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var result = services.AddCodeGenerator(configuration);

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddCodeGenerator_WithCustomOptions_ShouldReturnSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddCodeGenerator(options => { });

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddCodeGenerator_ShouldRegisterServiceAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddCodeGenerator(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service1 = serviceProvider.GetService<ICodeGeneratorService>();
        var service2 = serviceProvider.GetService<ICodeGeneratorService>();
        
        service1.Should().BeSameAs(service2); // Singleton should return same instance
    }

    [Fact]
    public void AddCodeGenerator_ShouldAllowMethodChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var result = services
            .AddCodeGenerator(configuration)
            .AddSingleton<IConfiguration>(configuration); // Chain additional services

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddCodeGenerator_WithEmptyConfiguration_ShouldUseDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddCodeGenerator(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<CodeGeneratorOptions>>();

        // Assert
        options.Should().NotBeNull();
        options!.Value.DefaultLength.Should().Be(6); // Default value
        options.Value.VerificationCodeLength.Should().Be(6);
        options.Value.ApiKeyLength.Should().Be(32);
    }

    [Fact]
    public void AddCodeGenerator_ServiceCanGenerateCodes()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddCodeGenerator(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var codeGenerator = serviceProvider.GetRequiredService<ICodeGeneratorService>();

        // Act
        var code = codeGenerator.Generate(10);

        // Assert
        code.Should().NotBeNullOrEmpty();
        code.Length.Should().Be(10);
    }
}
