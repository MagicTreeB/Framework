using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace MMO.Core.Extensions;

/// <summary>
/// Extension methods for automatic endpoint discovery and mapping
/// </summary>
public static class EndpointExtension
{
    /// <summary>
    /// Automatically discovers and maps all endpoint extension methods in the ApiEndpoints namespace.
    /// Convention: Methods must be named "Map*Endpoints" and be static extension methods of IEndpointRouteBuilder
    /// </summary>
    /// <param name="app">The endpoint route builder</param>
    /// <param name="apiEndpointsNamespace">The namespace to search for endpoint classes (e.g., "YourApi.Api.ApiEndpoints")</param>
    /// <returns>The endpoint route builder for chaining</returns>
    public static IEndpointRouteBuilder MapAllEndpoints(this IEndpointRouteBuilder app, string apiEndpointsNamespace)
    {
        var assembly = Assembly.GetCallingAssembly();
        
        // Find all types in the specified ApiEndpoints namespace
        var endpointTypes = assembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith(apiEndpointsNamespace) == true 
                       && t.IsClass 
                       && t.IsAbstract 
                       && t.IsSealed); // Static classes are abstract and sealed
        
        foreach (var type in endpointTypes)
        {
            // Find all static methods that match the naming convention "Map*Endpoints"
            var mapMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name.StartsWith("Map") 
                           && m.Name.EndsWith("Endpoints")
                           && m.GetParameters().Length == 1
                           && m.GetParameters()[0].ParameterType == typeof(IEndpointRouteBuilder)
                           && m.ReturnType == typeof(void));
            
            foreach (var method in mapMethods)
            {
                try
                {
                    // Invoke the static method with the IEndpointRouteBuilder instance
                    method.Invoke(null, new object[] { app });
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to map endpoints from {type.Name}.{method.Name}: {ex.Message}", 
                        ex);
                }
            }
        }
        
        return app;
    }
}
