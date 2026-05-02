using System.Text.Json;

namespace MagicTree.Framework.Middlewares.NullResponse;

/// <summary>
/// Configuration options for NullResponseMiddleware
/// </summary>
public class NullResponseOptions
{
    /// <summary>
    /// Whether to remove null properties from JSON responses
    /// Default: true
    /// </summary>
    public bool RemoveNullProperties { get; set; } = true;

    /// <summary>
    /// JSON serializer options for re-serialization
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
