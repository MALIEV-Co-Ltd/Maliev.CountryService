using System.Text.Json;

namespace Maliev.CountryService.Application.Interfaces;

/// <summary>
/// Helper class for JSON serialization and deserialization with consistent options.
/// </summary>
public static class JsonHelper
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Deserializes a JSON string to the specified type.
    /// </summary>
    public static T? Deserialize<T>(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return default;

        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }

    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    public static string Serialize<T>(T? obj)
    {
        if (obj == null)
            return string.Empty;

        return JsonSerializer.Serialize(obj, DefaultOptions);
    }

    /// <summary>
    /// Gets the default JSON serializer options.
    /// </summary>
    public static JsonSerializerOptions GetOptions() => DefaultOptions;
}
