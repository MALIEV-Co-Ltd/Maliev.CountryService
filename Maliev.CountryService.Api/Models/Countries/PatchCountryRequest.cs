namespace Maliev.CountryService.Api.Models.Countries;

/// <summary>
/// Request model for partially updating an existing country. All properties are optional.
/// </summary>
public class PatchCountryRequest
{
    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-2 code (2-letter code). Optional.
    /// </summary>
    public string? Iso2 { get; set; }
    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-3 code (3-letter code). Optional.
    /// </summary>
    public string? Iso3 { get; set; }
    /// <summary>
    /// Gets or sets the common English name of the country. Optional.
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// Gets or sets the official English name of the country. Optional.
    /// </summary>
    public string? OfficialName { get; set; }
    /// <summary>
    /// Gets or sets the ISO 3166-1 numeric code (3-digit string). Optional.
    /// </summary>
    public string? NumericCode { get; set; }
    /// <summary>
    /// Gets or sets the capital city of the country. Optional.
    /// </summary>
    public string? Capital { get; set; }
    /// <summary>
    /// Gets or sets the geographic or political region of the country (e.g., "Europe", "Asia"). Optional.
    /// </summary>
    public string? Region { get; set; }
    /// <summary>
    /// Gets or sets the more specific geographic area of the country (e.g., "Western Europe", "Southeast Asia"). Optional.
    /// </summary>
    public string? Subregion { get; set; }
    /// <summary>
    /// Gets or sets the latitude of the country's center. Optional.
    /// </summary>
    public double? Latitude { get; set; }
    /// <summary>
    /// Gets or sets the longitude of the country's center. Optional.
    /// </summary>
    public double? Longitude { get; set; }
    /// <summary>
    /// Gets or sets the demonym for the country (e.g., "American", "Thai"). Optional.
    /// </summary>
    public string? Demonym { get; set; }
    /// <summary>
    /// Gets or sets the area of the country in square kilometers. Optional.
    /// </summary>
    public double? AreaKm2 { get; set; }
    /// <summary>
    /// Gets or sets the estimated population of the country. Optional.
    /// </summary>
    public long? Population { get; set; }
    /// <summary>
    /// Gets or sets the Gini coefficient, a measure of income inequality. Optional.
    /// </summary>
    public double? GiniCoefficient { get; set; }
    /// <summary>
    /// Gets or sets the list of IANA timezone identifiers as a JSON array string. Optional.
    /// </summary>
    public string? Timezones { get; set; } // Patch means optional
    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-3 codes of bordering countries as a JSON array string. Optional.
    /// </summary>
    public string? Borders { get; set; } // Patch means optional
    /// <summary>
    /// Gets or sets the list of international direct dialing codes as a JSON array string. Optional.
    /// </summary>
    public string? CallingCodes { get; set; } // Patch means optional
    /// <summary>
    /// Gets or sets the list of top-level domains as a JSON array string. Optional.
    /// </summary>
    public string? TopLevelDomains { get; set; } // Patch means optional
    /// <summary>
    /// Gets or sets the currencies used in the country as a JSON object string. Optional.
    /// </summary>
    public string? Currencies { get; set; } // Patch means optional
    /// <summary>
    /// Gets or sets the languages spoken in the country as a JSON object string. Optional.
    /// </summary>
    public string? Languages { get; set; } // Patch means optional
    /// <summary>
    /// Gets or sets the country name translations as a JSON object string. Optional.
    /// </summary>
    public string? Translations { get; set; } // Patch means optional
    /// <summary>
    /// Gets or sets the flag URLs (SVG and PNG) as a JSON object string. Optional.
    /// </summary>
    public string? Flags { get; set; } // Patch means optional
    /// <summary>
    /// Gets or sets the coat of arms URLs (SVG and PNG) as a JSON object string. Optional.
    /// </summary>
    public string? CoatOfArms { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the country is independent. Optional.
    /// </summary>
    public bool? Independent { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the country is a UN member. Optional.
    /// </summary>
    public bool? UnMember { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the country is landlocked. Optional.
    /// </summary>
    public bool? Landlocked { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the country record is active. Optional.
    /// </summary>
    public bool? IsActive { get; set; }
}