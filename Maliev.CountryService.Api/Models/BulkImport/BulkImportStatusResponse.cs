namespace Maliev.CountryService.Api.Models.BulkImport;

/// <summary>
/// Response model for bulk import job status.
/// </summary>
public class BulkImportStatusResponse
{
    /// <summary>
    /// Gets or sets the unique job identifier.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Gets or sets the job status (Pending, Validating, Validated, Processing, Completed, Failed).
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the total number of records in the import.
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Gets or sets the number of records processed so far.
    /// </summary>
    public int ProcessedRecords { get; set; }

    /// <summary>
    /// Gets or sets the list of validation errors.
    /// </summary>
    public List<ValidationErrorResponse> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Gets or sets the UTC timestamp when the job was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the job started processing.
    /// </summary>
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the job completed.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the job duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }
}
