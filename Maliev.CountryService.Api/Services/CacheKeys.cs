namespace Maliev.CountryService.Api.Services;

public static class CacheKeys
{
    public const string Continents = "country:continents";
    
    public static string CountryById(int id) => $"country:id:{id}";
    
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
    
    public static string SearchPrefix = "country:search";
    public static string CountryPrefix = "country:id";
}