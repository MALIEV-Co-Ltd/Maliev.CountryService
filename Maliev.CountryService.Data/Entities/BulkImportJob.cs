using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.CountryService.Data.Entities;

public class BulkImportJob
{
    public long Id { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Validating, Processed, Completed, Failed

    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int FailedRecords { get; set; }

    [Column(TypeName = "jsonb")]
    public string? ValidationErrors { get; set; } // JSON string of validation errors

    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    [Column(TypeName = "jsonb")]
    public string? PayloadData { get; set; } // Stores the serialized BulkImportRequest
}