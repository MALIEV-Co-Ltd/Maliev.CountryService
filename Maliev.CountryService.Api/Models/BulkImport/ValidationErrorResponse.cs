namespace Maliev.CountryService.Api.Models.BulkImport;

/// <summary>
/// T106: Validation error details for bulk import.
/// </summary>
public class ValidationErrorResponse
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = null!;
    public string Message { get; set; } = null!;
}
