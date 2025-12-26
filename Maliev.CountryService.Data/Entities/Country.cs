using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.CountryService.Data.Entities;

public class Country
{
    public long Id { get; set; }

    [Required, StringLength(2)]
    public string Iso2 { get; set; } = string.Empty;

    [StringLength(3)]
    public string? Iso3 { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? OfficialName { get; set; }

    [StringLength(3)]
    public string? NumericCode { get; set; }

    [StringLength(100)]
    public string? Capital { get; set; }

    [StringLength(50)]
    public string? Region { get; set; }

    [StringLength(50)]
    public string? Subregion { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [StringLength(50)]
    public string? Demonym { get; set; }

    public double? AreaKm2 { get; set; }
    public long? Population { get; set; }
    public double? GiniCoefficient { get; set; }

    // JSONB fields, stored as string in EF Core
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

    [ConcurrencyCheck]
    public Guid Version { get; set; }

    [Required, StringLength(100)]
    public string CreatedBy { get; set; } = "system"; // Default to system for now
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Required, StringLength(100)]
    public string UpdatedBy { get; set; } = "system"; // Default to system for now
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
