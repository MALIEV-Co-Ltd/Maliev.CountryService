using System.ComponentModel.DataAnnotations;

namespace Maliev.CountryService.Api.Models;

/// <summary>
/// Request model for searching countries with filtering and pagination.
/// </summary>
public class CountrySearchRequest
{
    /// <summary>
    /// Gets or sets the country name to filter by.
    /// </summary>
    [MaxLength(100)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the continent to filter by.
    /// </summary>
    [MaxLength(50)]
    public string? Continent { get; set; }

    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-2 code to filter by.
    /// </summary>
    [MaxLength(2)]
    [RegularExpression(@"^[A-Z]{2}$", ErrorMessage = "ISO2 must be exactly 2 uppercase letters")]
    public string? ISO2 { get; set; }

    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-3 code to filter by.
    /// </summary>
    [MaxLength(3)]
    [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "ISO3 must be exactly 3 uppercase letters")]
    public string? ISO3 { get; set; }

    /// <summary>
    /// Gets or sets the country code to filter by.
    /// </summary>
    [MaxLength(20)]
    [RegularExpression(@"^[\d\-\+]+$", ErrorMessage = "CountryCode can only contain digits, dashes, and plus signs")]
    public string? CountryCode { get; set; }

    /// <summary>
    /// Gets or sets the page size (number of results per page).
    /// </summary>
    [Range(1, 1000, ErrorMessage = "PageSize must be between 1 and 1000")]
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the page number to retrieve.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the field to sort by.
    /// </summary>
    [RegularExpression(@"^(Name|Continent|CountryCode|ISO2|ISO3|CreatedDate|ModifiedDate)$",
        ErrorMessage = "SortBy must be one of: Name, Continent, CountryCode, ISO2, ISO3, CreatedDate, ModifiedDate")]
    public string? SortBy { get; set; } = "Name";

    /// <summary>
    /// Gets or sets the sort direction (asc or desc).
    /// </summary>
    [RegularExpression(@"^(asc|desc)$", ErrorMessage = "SortDirection must be either 'asc' or 'desc'")]
    public string? SortDirection { get; set; } = "asc";
}
