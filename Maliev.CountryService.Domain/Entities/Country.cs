using System.ComponentModel.DataAnnotations;

namespace Maliev.CountryService.Domain.Entities;

/// <summary>
/// Represents a country entity with geographical and metadata information.
/// </summary>
public class Country
{
    /// <summary>
    /// Gets or sets the unique identifier of the country.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-2 code (2-letter code).
    /// </summary>
    [Required, StringLength(2)]
    public string Iso2 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISO 3166-1 alpha-3 code (3-letter code).
    /// </summary>
    [StringLength(3)]
    public string? Iso3 { get; set; }

    /// <summary>
    /// Gets or sets the common English name of the country.
    /// </summary>
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the official English name of the country.
    /// </summary>
    [StringLength(200)]
    public string? OfficialName { get; set; }

    /// <summary>
    /// Gets or sets the ISO 3166-1 numeric code (3-digit string).
    /// </summary>
    [StringLength(3)]
    public string? NumericCode { get; set; }

    /// <summary>
    /// Gets or sets the capital city of the country.
    /// </summary>
    [StringLength(100)]
    public string? Capital { get; set; }

    /// <summary>
    /// Gets or sets the geographic or political region of the country.
    /// </summary>
    [StringLength(50)]
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the more specific geographic area of the country.
    /// </summary>
    [StringLength(50)]
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
    [StringLength(50)]
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
    /// Gets or sets the IANA timezone identifiers stored as a JSON array string.
    /// </summary>
    public string Timezones { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the ISO alpha-3 codes of bordering countries stored as a JSON array string.
    /// </summary>
    public string Borders { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the international direct dialing codes stored as a JSON array string.
    /// </summary>
    public string CallingCodes { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the top-level domains stored as a JSON array string.
    /// </summary>
    public string TopLevelDomains { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the currencies used in the country stored as a JSON object string.
    /// </summary>
    public string Currencies { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the languages spoken in the country stored as a JSON object string.
    /// </summary>
    public string Languages { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the country name translations stored as a JSON object string.
    /// </summary>
    public string Translations { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the flag URLs (SVG and PNG) stored as a JSON object string.
    /// </summary>
    public string Flags { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the coat of arms URLs (SVG and PNG) stored as a JSON object string.
    /// </summary>
    public string? CoatOfArms { get; set; }

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
    /// Gets or sets a value indicating whether the country record is active (soft delete flag).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the identifier of the user who created this record.
    /// </summary>
    [Required, StringLength(100)]
    public string CreatedBy { get; set; } = "system";

    /// <summary>
    /// Gets or sets the UTC timestamp when the record was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the user who last updated this record.
    /// </summary>
    [Required, StringLength(100)]
    public string UpdatedBy { get; set; } = "system";

    /// <summary>
    /// Gets or sets the UTC timestamp when the record was last modified.
    /// </summary>
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the version number for optimistic concurrency control.
    /// </summary>
    public uint Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the UTC timestamp when the record was soft-deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}
