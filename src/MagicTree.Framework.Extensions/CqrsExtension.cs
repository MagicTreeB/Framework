using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MagicTree.Framework.Extensions;

/// <summary>
/// Extension methods for automatic CQRS handler registration
/// </summary>
public static class CqrsExtension
{
    /// <summary>
    /// Automatically registers all CQRS command and query handlers from the specified assembly.
    /// Discovers and registers implementations of:
    /// - Fluents.Requests.IHandler&lt;TCommand, TResponse&gt; (Command handlers)
    /// - Fluents.Queries.IHandler&lt;TQuery, TResponse&gt; (Query handlers)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">Assemblies to scan for handlers. If none provided, scans calling assembly.</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCqrsHandlers(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        // If no assemblies provided, use calling assembly
        if (assemblies.Length == 0)
        {
            assemblies = new[] { Assembly.GetCallingAssembly() };
        }

        foreach (var assembly in assemblies)
        {
            // Find all types in the assembly
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract);

            foreach (var implementationType in types)
            {
                // Get all interfaces the type implements
                var interfaces = implementationType.GetInterfaces();

                foreach (var interfaceType in interfaces)
                {
                    // Check if it's a generic interface
                    if (!interfaceType.IsGenericType)
                        continue;

                    var genericDefinition = interfaceType.GetGenericTypeDefinition();
                    var genericTypeName = genericDefinition.FullName;

                    // Check if it's a CQRS handler interface
                    // Support both Fluents.* and DKNet.SlimBus.Extensions.Fluents.* namespaces
                    if (genericTypeName == "Fluents.Requests.IHandler`2" || 
                        genericTypeName == "Fluents.Queries.IHandler`2" ||
                        genericTypeName == "DKNet.SlimBus.Extensions.Fluents+Requests+IHandler`2" ||
                        genericTypeName == "DKNet.SlimBus.Extensions.Fluents+Queries+IHandler`2")
                    {
                        // Register as scoped
                        services.AddScoped(interfaceType, implementationType);
                    }
                }
            }
        }

        return services;
    }

    /// <summary>
    /// Automatically registers all CQRS handlers from assemblies containing the specified marker types.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="markerTypes">Types whose assemblies should be scanned</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCqrsHandlersFromAssemblyContaining(
        this IServiceCollection services,
        params Type[] markerTypes)
    {
        var assemblies = markerTypes
            .Select(t => t.Assembly)
            .Distinct()
            .ToArray();

        return services.AddCqrsHandlers(assemblies);
    }
}
