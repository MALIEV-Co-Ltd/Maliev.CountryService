namespace Maliev.CountryService.Data.Models;

/// <summary>
/// Tracks bulk import operations with status, validation errors, and processing metrics.
/// </summary>
public class BulkImportJob
{
    public long Id { get; set; }

    /// <summary>
    /// Job status: Pending, Validating, Validated, ValidationFailed, Processing, Completed, Failed
    /// </summary>
    public string Status { get; set; } = "Pending";

    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int FailedRecords { get; set; }

    /// <summary>
    /// JSON array of validation error objects with rowNumber, field, message
    /// </summary>
    public string ValidationErrors { get; set; } = "[]";

    public string? ErrorMessage { get; set; }

    public string UserId { get; set; } = null!;
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public Guid? CorrelationId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Computed property: duration in milliseconds
    /// </summary>
    public long? DurationMs =>
        CompletedAtUtc.HasValue && StartedAtUtc.HasValue
            ? (long)(CompletedAtUtc.Value - StartedAtUtc.Value).TotalMilliseconds
            : null;
}
