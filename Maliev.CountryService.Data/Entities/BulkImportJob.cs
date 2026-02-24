using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.CountryService.Data.Entities;

public class BulkImportJob
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Validating, Processed, Completed, Failed

    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int FailedRecords { get; set; }

    [Column(TypeName = "jsonb")]
    public string? ValidationErrors { get; set; } // JSON string of validation errors

    public string? ErrorMessage { get; set; }

    [Required]
    [StringLength(100)]
    public string UserId { get; set; } = string.Empty;

    public string? UserEmail { get; set; }

    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public Guid? ClaimedByWorkerId { get; set; }

    public string? IpAddress { get; set; }
    public Guid? CorrelationId { get; set; }

    [NotMapped]
    public long? DurationMs => (CompletedAtUtc.HasValue && StartedAtUtc.HasValue)
        ? (long?)(CompletedAtUtc.Value - StartedAtUtc.Value).TotalMilliseconds
        : null;

    [Column(TypeName = "jsonb")]
    public string? PayloadData { get; set; } // Stores the serialized BulkImportRequest
}
