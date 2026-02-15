using System.ComponentModel.DataAnnotations;

namespace Maliev.CountryService.Api.Models.Countries;

/// <summary>
/// Request model for creating a new country.
/// </summary>
public class CreateCountryRequest
{
    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-2 code (2-letter code). Required.
    /// </summary>
    [Required(ErrorMessage = "ISO2 code is required.")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "ISO2 code must be 2 characters.")]
    [RegularExpression("^[A-Z]{2}$", ErrorMessage = "ISO2 code must consist of 2 uppercase letters.")]
    [System.Text.Json.Serialization.JsonPropertyName("iso2")]
    public string Iso2 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-3 code (3-letter code). Required.
    /// </summary>
    [Required(ErrorMessage = "ISO3 code is required.")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "ISO3 code must be 3 characters.")]
    [RegularExpression("^[A-Z]{3}$", ErrorMessage = "ISO3 code must consist of 3 uppercase letters.")]
    [System.Text.Json.Serialization.JsonPropertyName("iso3")]
    public string Iso3 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the common English name of the country. Required.
    /// </summary>
    [Required(ErrorMessage = "Country name is required.")]
    [StringLength(100, ErrorMessage = "Country name cannot exceed 100 characters.")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the official English name of the country. Optional.
    /// </summary>
    [StringLength(200, ErrorMessage = "Official name cannot exceed 200 characters.")]
    [System.Text.Json.Serialization.JsonPropertyName("officialName")]
    public string? OfficialName { get; set; }

    /// <summary>
    /// Gets or sets the ISO 3166-1 numeric code (3-digit string). Optional.
    /// </summary>
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Numeric code must be 3 digits.")]
    [RegularExpression("^[0-9]{3}$", ErrorMessage = "Numeric code must consist of 3 digits.")]
    [System.Text.Json.Serialization.JsonPropertyName("numericCode")]
    public string? NumericCode { get; set; }

    /// <summary>
    /// Gets or sets the capital city of the country. Optional.
    /// </summary>
    [StringLength(100, ErrorMessage = "Capital name cannot exceed 100 characters.")]
    [System.Text.Json.Serialization.JsonPropertyName("capital")]
    public string? Capital { get; set; }

    /// <summary>
    /// Gets or sets the geographic or political region of the country (e.g., "Europe", "Asia"). Optional.
    /// </summary>
    [StringLength(50, ErrorMessage = "Region name cannot exceed 50 characters.")]
    [System.Text.Json.Serialization.JsonPropertyName("region")]
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the more specific geographic area of the country (e.g., "Western Europe", "Southeast Asia"). Optional.
    /// </summary>
    [StringLength(50, ErrorMessage = "Subregion name cannot exceed 50 characters.")]
    [System.Text.Json.Serialization.JsonPropertyName("subregion")]
    public string? Subregion { get; set; }

    /// <summary>
    /// Gets or sets the latitude of the country's center. Optional.
    /// </summary>
    [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
    [System.Text.Json.Serialization.JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    /// <summary>
    /// Gets or sets the longitude of the country's center. Optional.
    /// </summary>
    [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
    [System.Text.Json.Serialization.JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    /// <summary>
    /// Gets or sets the demonym for the country (e.g., "American", "Thai"). Optional.
    /// </summary>
    [StringLength(50, ErrorMessage = "Demonym cannot exceed 50 characters.")]
    [System.Text.Json.Serialization.JsonPropertyName("demonym")]
    public string? Demonym { get; set; }

    /// <summary>
    /// Gets or sets the area of the country in square kilometers. Optional.
    /// </summary>
    [Range(0.0, double.MaxValue, ErrorMessage = "Area cannot be negative.")]
    [System.Text.Json.Serialization.JsonPropertyName("areaKm2")]
    public double? AreaKm2 { get; set; }

    /// <summary>
    /// Gets or sets the estimated population of the country. Optional.
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "Population cannot be negative.")]
    [System.Text.Json.Serialization.JsonPropertyName("population")]
    public long? Population { get; set; }

    /// <summary>
    /// Gets or sets the Gini coefficient, a measure of income inequality. Optional.
    /// </summary>
    [Range(0.0, 100.0, ErrorMessage = "Gini coefficient must be between 0 and 100.")]
    [System.Text.Json.Serialization.JsonPropertyName("giniCoefficient")]
    public double? GiniCoefficient { get; set; }

    /// <summary>
    /// Gets or sets the list of IANA timezone identifiers as a JSON array string. Required.
    /// </summary>
    [Required(ErrorMessage = "Timezones are required.")]
    [System.Text.Json.Serialization.JsonPropertyName("timezones")]
    public string Timezones { get; set; } = "[]"; // Non-nullable, default empty JSON array

    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-3 codes of bordering countries as a JSON array string. Required.
    /// </summary>
    [Required(ErrorMessage = "Borders are required.")]
    [System.Text.Json.Serialization.JsonPropertyName("borders")]
    public string Borders { get; set; } = "[]"; // Non-nullable, default empty JSON array

    /// <summary>
    /// Gets or sets the list of international direct dialing codes as a JSON array string. Required.
    /// </summary>
    [Required(ErrorMessage = "Calling codes are required.")]
    [System.Text.Json.Serialization.JsonPropertyName("callingCodes")]
    public string CallingCodes { get; set; } = "[]"; // Non-nullable, default empty JSON array

    /// <summary>
    /// Gets or sets the list of top-level domains as a JSON array string. Required.
    /// </summary>
    [Required(ErrorMessage = "Top level domains are required.")]
    [System.Text.Json.Serialization.JsonPropertyName("topLevelDomains")]
    public string TopLevelDomains { get; set; } = "[]"; // Non-nullable, default empty JSON array

    /// <summary>
    /// Gets or sets the currencies used in the country as a JSON object string. Required.
    /// </summary>
    [Required(ErrorMessage = "Currencies are required.")]
    [System.Text.Json.Serialization.JsonPropertyName("currencies")]
    public string Currencies { get; set; } = "{}"; // Non-nullable, default empty JSON object

    /// <summary>
    /// Gets or sets the languages spoken in the country as a JSON object string. Required.
    /// </summary>
    [Required(ErrorMessage = "Languages are required.")]
    [System.Text.Json.Serialization.JsonPropertyName("languages")]
    public string Languages { get; set; } = "{}"; // Non-nullable, default empty JSON object

    /// <summary>
    /// Gets or sets the country name translations as a JSON object string. Required.
    /// </summary>
    [Required(ErrorMessage = "Translations are required.")]
    [System.Text.Json.Serialization.JsonPropertyName("translations")]
    public string Translations { get; set; } = "{}"; // Non-nullable, default empty JSON object

    /// <summary>
    /// Gets or sets the flag URLs (SVG and PNG) as a JSON object string. Required.
    /// </summary>
    [Required(ErrorMessage = "Flags are required.")]
    [System.Text.Json.Serialization.JsonPropertyName("flags")]
    public string Flags { get; set; } = "{}"; // Non-nullable, default empty JSON object

    /// <summary>
    /// Gets or sets the coat of arms URLs (SVG and PNG) as a JSON object string. Optional.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("coatOfArms")]
    public string? CoatOfArms { get; set; } // Can be null as per spec
    /// <summary>
    /// Gets or sets a value indicating whether the country is independent. Optional.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("independent")]
    public bool? Independent { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the country is a UN member. Optional.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("unMember")]
    public bool? UnMember { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the country is landlocked. Optional.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("landlocked")]
    public bool? Landlocked { get; set; }
}
