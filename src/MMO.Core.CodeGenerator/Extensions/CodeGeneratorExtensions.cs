using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MMO.Core.CodeGenerator.Interfaces;
using MMO.Core.CodeGenerator.Options;
using MMO.Core.CodeGenerator.Services;

namespace MMO.Core.CodeGenerator.Extensions;

/// <summary>
/// Extension methods for registering code generator services
/// </summary>
public static class CodeGeneratorExtensions
{
    /// <summary>
    /// Add code generator services to the dependency injection container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration containing CodeGenerator section</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddCodeGenerator(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register options
        services.Configure<CodeGeneratorOptions>(
            configuration.GetSection(CodeGeneratorOptions.SectionName));

        // Register service
        services.AddSingleton<ICodeGeneratorService, CodeGeneratorService>();

        return services;
    }

    /// <summary>
    /// Add code generator services with custom options
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureOptions">Action to configure options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddCodeGenerator(
        this IServiceCollection services,
        Action<CodeGeneratorOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddSingleton<ICodeGeneratorService, CodeGeneratorService>();

        return services;
    }
}
