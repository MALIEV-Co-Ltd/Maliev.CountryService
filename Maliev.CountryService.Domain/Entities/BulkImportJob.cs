using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.CountryService.Domain.Entities;

/// <summary>
/// Represents a bulk import job for processing multiple countries in a batch.
/// </summary>
public class BulkImportJob
{
    /// <summary>
    /// Gets or sets the unique identifier of the bulk import job.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the current status of the job (Pending, Validating, Validated, Processing, Completed, Failed).
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Gets or sets the total number of records in the import batch.
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Gets or sets the number of records successfully processed.
    /// </summary>
    public int ProcessedRecords { get; set; }

    /// <summary>
    /// Gets or sets the number of records that failed processing.
    /// </summary>
    public int FailedRecords { get; set; }

    /// <summary>
    /// Gets or sets the validation errors as a JSON string.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? ValidationErrors { get; set; }

    /// <summary>
    /// Gets or sets the error message if the job failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who submitted the import job.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email of the user who submitted the import job.
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created this job.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the job was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp when the job started processing.
    /// </summary>
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the job completed.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the worker ID that claimed this job for atomic processing.
    /// </summary>
    public Guid? ClaimedByWorkerId { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the user who submitted the job.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for request tracing.
    /// </summary>
    public Guid? CorrelationId { get; set; }

    /// <summary>
    /// Gets the job duration in milliseconds, computed from start and completion timestamps.
    /// </summary>
    [NotMapped]
    public long? DurationMs => (CompletedAtUtc.HasValue && StartedAtUtc.HasValue)
        ? (long?)(CompletedAtUtc.Value - StartedAtUtc.Value).TotalMilliseconds
        : null;

    /// <summary>
    /// Gets or sets the serialized import payload data for async processing.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? PayloadData { get; set; }
}
