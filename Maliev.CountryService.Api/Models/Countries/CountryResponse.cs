using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Maliev.CountryService.Api.Models.Countries;

/// <summary>
/// Data Transfer Object for country information.
/// </summary>
public class CountryResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the country.
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-2 code (2-letter code).
    /// </summary>
    [Required]
    public string Iso2 { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-3 code (3-letter code).
    /// </summary>
    public string? Iso3 { get; set; }
    
    /// <summary>
    /// Gets or sets the common English name of the country.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the official English name of the country.
    /// </summary>
    public string? OfficialName { get; set; }

    /// <summary>
    /// Gets or sets the ISO 3166-1 numeric code (3-digit string).
    /// </summary>
    public string? NumericCode { get; set; }

    /// <summary>
    /// Gets or sets the capital city of the country.
    /// </summary>
    public string? Capital { get; set; }

    /// <summary>
    /// Gets or sets the geographic or political region of the country (e.g., "Europe", "Asia").
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the more specific geographic area of the country (e.g., "Western Europe", "Southeast Asia").
    /// </summary>
    public string? Subregion { get; set; }

    /// <summary>
    /// Gets or sets the latitude of the country's center.
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Gets or sets the longitude of the country's center.
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// Gets or sets the demonym for the country (e.g., "American", "Thai").
    /// </summary>
    public string? Demonym { get; set; }

    /// <summary>
    /// Gets or sets the area of the country in square kilometers.
    /// </summary>
    public double? AreaKm2 { get; set; }

    /// <summary>
    /// Gets or sets the estimated population of the country.
    /// </summary>
    public long? Population { get; set; }

    /// <summary>
    /// Gets or sets the Gini coefficient, a measure of income inequality.
    /// </summary>
    public double? GiniCoefficient { get; set; }
    
    /// <summary>
    /// Gets or sets the list of IANA timezone identifiers as a JSON array.
    /// </summary>
    public JsonElement Timezones { get; set; }

    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-3 codes of bordering countries as a JSON array.
    /// </summary>
    public JsonElement Borders { get; set; }

    /// <summary>
    /// Gets or sets the list of international direct dialing codes as a JSON array.
    /// </summary>
    public JsonElement CallingCodes { get; set; }

    /// <summary>
    /// Gets or sets the list of top-level domains as a JSON array.
    /// </summary>
    public JsonElement TopLevelDomains { get; set; }

    /// <summary>
    /// Gets or sets the currencies used in the country as a JSON object.
    /// </summary>
    public JsonElement Currencies { get; set; }

    /// <summary>
    /// Gets or sets the languages spoken in the country as a JSON object.
    /// </summary>
    public JsonElement Languages { get; set; }

    /// <summary>
    /// Gets or sets the country name translations as a JSON object.
    /// </summary>
    public JsonElement Translations { get; set; }

    /// <summary>
    /// Gets or sets the flag URLs (SVG and PNG) as a JSON object.
    /// </summary>
    public JsonElement Flags { get; set; }

    /// <summary>
    /// Gets or sets the coat of arms URLs (SVG and PNG) as a JSON object.
    /// </summary>
    public JsonElement? CoatOfArms { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the country is independent.
    /// </summary>
    public bool Independent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the country is a UN member.
    /// </summary>
    public bool UnMember { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the country is landlocked.
    /// </summary>
    public bool Landlocked { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the country record is active.
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Gets or sets the UTC timestamp when the country record was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the country record was last modified.
    /// </summary>
    public DateTime LastModifiedUtc { get; set; }

    /// <summary>
    /// Gets or sets the ETag for optimistic concurrency control.
    /// </summary>
    public string ETag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the response was served from cache.
    /// </summary>
    [JsonIgnore]
    public bool XServedFromCache { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the cached response is stale.
    /// </summary>
    [JsonIgnore]
    public bool XCacheStale { get; set; }
}
