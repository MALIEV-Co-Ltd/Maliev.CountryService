namespace Maliev.CountryService.Api.Models.Countries;

/// <summary>
/// Response DTO for country data with computed ETag from version field.
/// </summary>
public class CountryResponse
{
    public long Id { get; set; }
    public string Iso2 { get; set; } = null!;
    public string Iso3 { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? OfficialName { get; set; }
    public string? NumericCode { get; set; }
    public string? Capital { get; set; }
    public string? Region { get; set; }
    public string? Subregion { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public string? Demonym { get; set; }
    public decimal? AreaKm2 { get; set; }
    public long? Population { get; set; }
    public decimal? GiniCoefficient { get; set; }

    // JSONB fields as JSON strings
    public string Timezones { get; set; } = "[]";
    public string Borders { get; set; } = "[]";
    public string CallingCodes { get; set; } = "[]";
    public string TopLevelDomains { get; set; } = "[]";
    public string Currencies { get; set; } = "{}";
    public string Languages { get; set; } = "{}";
    public string Translations { get; set; } = "{}";
    public string Flags { get; set; } = "{}";
    public string? CoatOfArms { get; set; }

    public bool Independent { get; set; }
    public bool UnMember { get; set; }
    public bool Landlocked { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }

    /// <summary>
    /// ETag computed from version UUID (Base64 encoded)
    /// </summary>
    public string ETag { get; set; } = null!;
}
