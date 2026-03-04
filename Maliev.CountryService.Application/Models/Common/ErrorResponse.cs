namespace Maliev.CountryService.Application.Models.Common;

/// <summary>
/// Represents a generic error response returned by the API.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets or sets the error code identifying the type of error.
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a human-readable description of the error.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
