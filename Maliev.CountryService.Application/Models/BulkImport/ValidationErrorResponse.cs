namespace Maliev.CountryService.Application.Models.BulkImport;

/// <summary>
/// Represents a validation error for a specific row and field in a bulk import operation.
/// </summary>
public class ValidationErrorResponse
{
    /// <summary>
    /// Gets or sets the row number in the import batch where the error occurred.
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Gets or sets the name of the field that failed validation.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the validation error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
