namespace Maliev.CountryService.Data.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public long? CountryId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Keep for backward compatibility
    public string UserId { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string UserRoles { get; set; } = "[]";
    public DateTime TimestampUtc { get; set; } // Keep for backward compatibility
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? Changes { get; set; } // JSONB
    public string? BeforeSnapshot { get; set; }
    public string AfterSnapshot { get; set; } = "{}";
    public string ChangedFields { get; set; } = "[]";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid? CorrelationId { get; set; }

    // Navigation property
    public Country? Country { get; set; }
}