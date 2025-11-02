namespace Maliev.CountryService.Api.Models.BulkImport;

/// <summary>
/// T105: Response for bulk import job status.
/// </summary>
public class BulkImportStatusResponse
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = null!; // Pending, Validating, Validated, Processing, Completed, Failed
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public List<ValidationErrorResponse> ValidationErrors { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public long? DurationMs { get; set; }
}
