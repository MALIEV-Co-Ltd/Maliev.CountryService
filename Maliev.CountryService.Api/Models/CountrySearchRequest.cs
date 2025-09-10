using System.ComponentModel.DataAnnotations;

namespace Maliev.CountryService.Api.Models;

public class CountrySearchRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(50)]
    public string? Continent { get; set; }

    [MaxLength(2)]
    [RegularExpression(@"^[A-Z]{2}$", ErrorMessage = "ISO2 must be exactly 2 uppercase letters")]
    public string? ISO2 { get; set; }

    [MaxLength(3)]
    [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "ISO3 must be exactly 3 uppercase letters")]
    public string? ISO3 { get; set; }

    [MaxLength(20)]
    [RegularExpression(@"^[\d\-\+]+$", ErrorMessage = "CountryCode can only contain digits, dashes, and plus signs")]
    public string? CountryCode { get; set; }

    [Range(1, 1000)]
    public int PageSize { get; set; } = 50;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    public string? SortBy { get; set; } = "Name";

    public string? SortDirection { get; set; } = "asc";
}