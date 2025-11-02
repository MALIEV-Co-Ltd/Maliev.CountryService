namespace Maliev.CountryService.Data.Models;

/// <summary>
/// Immutable audit trail for all country modification operations.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public long CountryId { get; set; }

    /// <summary>
    /// Operation type: Create, Update, Delete, BulkImport
    /// </summary>
    public string Operation { get; set; } = null!;

    public string UserId { get; set; } = null!;
    public string? UserEmail { get; set; }

    /// <summary>
    /// JSON array of user roles
    /// </summary>
    public string UserRoles { get; set; } = "[]";

    /// <summary>
    /// Full entity state before the change (null for Create operations)
    /// </summary>
    public string? BeforeSnapshot { get; set; }

    /// <summary>
    /// Full entity state after the change
    /// </summary>
    public string AfterSnapshot { get; set; } = null!;

    /// <summary>
    /// JSON array of field names that changed
    /// </summary>
    public string ChangedFields { get; set; } = "[]";

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid? CorrelationId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
