namespace Maliev.CountryService.Data.Models;

/// <summary>
/// Represents a country entity with all geographical, administrative, and metadata attributes.
/// </summary>
public class Country
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

    // JSONB fields stored as JSON strings (serialized/deserialized in service layer)
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
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Version UUID for optimistic concurrency control.
    /// </summary>
    public Guid Version { get; set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;
}
