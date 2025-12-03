namespace Maliev.CountryService.Data.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public long CountryId { get; set; }
    public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, HARD_DELETE
    public string UserId { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public string? Changes { get; set; } // JSONB
    public string? IpAddress { get; set; }

    // Navigation property
    public Country? Country { get; set; }
}