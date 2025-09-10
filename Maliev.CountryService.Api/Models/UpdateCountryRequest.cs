using System.ComponentModel.DataAnnotations;

namespace Maliev.CountryService.Api.Models;

public class UpdateCountryRequest
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Continent { get; set; }

    [Required]
    [MaxLength(20)]
    [RegularExpression(@"^[\d\-\+]+$", ErrorMessage = "CountryCode can only contain digits, dashes, and plus signs")]
    public required string CountryCode { get; set; }

    [Required]
    [MaxLength(2)]
    [RegularExpression(@"^[A-Z]{2}$", ErrorMessage = "ISO2 must be exactly 2 uppercase letters")]
    public required string ISO2 { get; set; }

    [Required]
    [MaxLength(3)]
    [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "ISO3 must be exactly 3 uppercase letters")]
    public required string ISO3 { get; set; }
}