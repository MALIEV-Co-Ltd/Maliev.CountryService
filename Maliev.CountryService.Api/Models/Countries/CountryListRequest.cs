namespace Maliev.CountryService.Api.Models.Countries;

/// <summary>
/// T068: Query parameters for country list endpoint with pagination, filtering, and sorting.
/// </summary>
public class CountryListRequest
{
    /// <summary>
    /// Page number (1-based). Default: 1
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size (max 100). Default: 20
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Filter by region (e.g., "Europe", "Asia")
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Filter by subregion (e.g., "Western Europe", "Eastern Asia")
    /// </summary>
    public string? Subregion { get; set; }

    /// <summary>
    /// Sort field: name, iso2, population, area. Default: name
    /// </summary>
    public string SortBy { get; set; } = "name";

    /// <summary>
    /// Sort order: asc, desc. Default: asc
    /// </summary>
    public string SortOrder { get; set; } = "asc";

    /// <summary>
    /// Include inactive countries. Default: false
    /// </summary>
    public bool IncludeInactive { get; set; } = false;
}
