namespace Maliev.CountryService.Api.Models;

/// <summary>
/// Data transfer object for country code information.
/// </summary>
public class CountryCodeDto
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the country code.
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the primary country code.
    /// </summary>
    public bool IsPrimary { get; set; }
}
