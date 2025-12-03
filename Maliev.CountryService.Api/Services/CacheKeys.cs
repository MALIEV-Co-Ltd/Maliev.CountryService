namespace Maliev.CountryService.Api.Services;

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
    public static string CountryById(int id) => $"country:id:{id}";

    /// <summary>
    /// Generates a cache key for country search results based on search parameters.
    /// </summary>
    /// <param name="name">The country name filter.</param>
    /// <param name="continent">The continent filter.</param>
    /// <param name="iso2">The ISO2 code filter.</param>
    /// <param name="iso3">The ISO3 code filter.</param>
    /// <param name="countryCode">The country code filter.</param>
    /// <param name="pageNumber">The page number for pagination.</param>
    /// <param name="pageSize">The page size for pagination.</param>
    /// <param name="sortBy">The field to sort by.</param>
    /// <param name="sortDirection">The sort direction (asc/desc).</param>
    /// <returns>A cache key string based on a hash of all search parameters.</returns>
    public static string CountrySearch(
        string? name,
        string? continent,
        string? iso2,
        string? iso3,
        string? countryCode,
        int pageNumber,
        int pageSize,
        string? sortBy,
        string? sortDirection)
    {
        // Using a hash of the search parameters to avoid very long cache keys
        // and to ensure consistent key generation
        var searchParams = $"{name}|{continent}|{iso2}|{iso3}|{countryCode}|{pageNumber}|{pageSize}|{sortBy}|{sortDirection}";
        return $"country:search:{searchParams.GetHashCode()}";
    }

    /// <summary>
    /// Cache key prefix for country search operations.
    /// </summary>
    public static string SearchPrefix = "country:search";

    /// <summary>
    /// Cache key prefix for country ID lookups.
    /// </summary>
    public static string CountryPrefix = "country:id";
}