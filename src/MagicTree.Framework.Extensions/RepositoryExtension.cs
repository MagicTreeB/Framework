using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MagicTree.Framework.Extensions;

/// <summary>
/// Extension methods for automatic repository and unit of work registration
/// </summary>
public static class RepositoryExtension
{
    /// <summary>
    /// Automatically registers all repository implementations and unit of work from the specified assemblies.
    /// Discovers and registers:
    /// - Classes implementing I*Repository interfaces
    /// - Classes implementing IUnitOfWork interface
    /// All registered as scoped services.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for implementations</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRepositories(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            assemblies = new[] { Assembly.GetCallingAssembly() };
        }

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract);

            foreach (var implementationType in types)
            {
                var interfaces = implementationType.GetInterfaces()
                    .Where(i => i.IsPublic);

                foreach (var interfaceType in interfaces)
                {
                    // Register repositories (interfaces ending with "Repository")
                    if (interfaceType.Name.EndsWith("Repository", StringComparison.Ordinal))
                    {
                        services.AddScoped(interfaceType, implementationType);
                    }
                    // Register unit of work
                    else if (interfaceType.Name == "IUnitOfWork")
                    {
                        services.AddScoped(interfaceType, implementationType);
                    }
                }
            }
        }

        return services;
    }

    /// <summary>
    /// Automatically registers all repositories and unit of work from assemblies containing the specified marker types.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="markerTypes">Types whose assemblies should be scanned</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRepositoriesFromAssemblyContaining(
        this IServiceCollection services,
        params Type[] markerTypes)
    {
        var assemblies = markerTypes
            .Select(t => t.Assembly)
            .Distinct()
            .ToArray();

        return services.AddRepositories(assemblies);
    }
}
