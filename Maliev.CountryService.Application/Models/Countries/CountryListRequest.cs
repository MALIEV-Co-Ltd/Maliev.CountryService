using System.ComponentModel.DataAnnotations;

namespace Maliev.CountryService.Application.Models.Countries;

/// <summary>
/// Request model for retrieving a paginated list of countries with filtering and sorting options.
/// </summary>
public class CountryListRequest
{
    /// <summary>
    /// Gets or sets the region to filter countries by.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the subregion to filter countries by.
    /// </summary>
    public string? Subregion { get; set; }

    /// <summary>
    /// Gets or sets the page number for pagination. If null, returns all items (subject to maximum limit).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int? Page { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page. If null, returns all items (subject to maximum limit).
    /// </summary>
    [Range(1, 1000, ErrorMessage = "Page size must be between 1 and 1000")]
    public int? PageSize { get; set; }

    /// <summary>
    /// Gets or sets the field to sort the results by. Default is "name".
    /// </summary>
    public string SortBy { get; set; } = "name";

    /// <summary>
    /// Gets or sets the sort order (e.g., "asc" for ascending, "desc" for descending). Default is "asc".
    /// </summary>
    public string SortOrder { get; set; } = "asc";

    /// <summary>
    /// Gets or sets a value indicating whether to include inactive countries in the results. Default is false.
    /// </summary>
    public bool IncludeInactive { get; set; } = false;
}
