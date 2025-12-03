namespace Maliev.CountryService.Api.Models.BulkImport;

/// <summary>
/// Validation error details for bulk import operations.
/// </summary>
public class ValidationErrorResponse
{
    /// <summary>
    /// Gets or sets the row number where the error occurred.
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Gets or sets the field name that failed validation.
    /// </summary>
    public string Field { get; set; } = null!;

    /// <summary>
    /// Gets or sets the validation error message.
    /// </summary>
    public string Message { get; set; } = null!;
}
