namespace Maliev.CountryService.Domain.Entities;

/// <summary>
/// Represents an audit log entry for tracking changes to country data.
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Gets or sets the unique identifier of the audit log entry.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the ID of the country this audit entry relates to.
    /// </summary>
    public Guid? CountryId { get; set; }

    /// <summary>
    /// Gets or sets the operation type (e.g., "CREATE", "UPDATE").
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action performed (kept for backward compatibility).
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the user who performed the action.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the user who performed the action.
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// Gets or sets the roles of the user who performed the action as a JSON array string.
    /// </summary>
    public string UserRoles { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the timestamp of the action (kept for backward compatibility).
    /// </summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the audit entry was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the JSON representation of the changes made.
    /// </summary>
    public string? Changes { get; set; }

    /// <summary>
    /// Gets or sets the JSON snapshot of the entity before the change.
    /// </summary>
    public string? BeforeSnapshot { get; set; }

    /// <summary>
    /// Gets or sets the JSON snapshot of the entity after the change.
    /// </summary>
    public string AfterSnapshot { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the list of changed field names as a JSON array string.
    /// </summary>
    public string ChangedFields { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the IP address of the requester.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string of the requester.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for request tracing.
    /// </summary>
    public Guid? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the associated country.
    /// </summary>
    public Country? Country { get; set; }
}
