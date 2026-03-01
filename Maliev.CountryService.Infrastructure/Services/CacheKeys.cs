namespace Maliev.CountryService.Infrastructure.Services;

/// <summary>
/// Provides standardized cache key generation for country-related data.
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Cache key for continent data.
    /// </summary>
    public const string Continents = "country:continents";

    /// <summary>
    /// Generates a cache key for a country lookup by ID.
    /// </summary>
    /// <param name="id">The country ID.</param>
    /// <returns>A cache key string in the format "country:id:{id}".</returns>
    public static string CountryById(Guid id) => $"country:id:{id}";

    /// <summary>
    /// Cache key prefix for country search operations.
    /// </summary>
    public static string SearchPrefix = "country:search";

    /// <summary>
    /// Cache key prefix for country ID lookups.
    /// </summary>
    public static string CountryPrefix = "country:id";
}
