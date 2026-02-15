using System.ComponentModel.DataAnnotations;

namespace Maliev.CountryService.Api.Models;

/// <summary>
/// Data transfer object for country information.
/// </summary>
public class CountryDto
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the country name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the continent.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string Continent { get; set; }

    /// <summary>
    /// Gets or sets the list of country codes.
    /// </summary>
    public List<CountryCodeDto> CountryCodes { get; set; } = new List<CountryCodeDto>();

    /// <summary>
    /// Gets the primary country code for backward compatibility.
    /// </summary>
    public string? CountryCode => CountryCodes.FirstOrDefault(c => c.IsPrimary)?.Code;

    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-2 code.
    /// </summary>
    [Required]
    [MaxLength(2)]
    public required string ISO2 { get; set; }

    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-3 code.
    /// </summary>
    [Required]
    [MaxLength(3)]
    public required string ISO3 { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the last modification date.
    /// </summary>
    public DateTime ModifiedDate { get; set; }
}
