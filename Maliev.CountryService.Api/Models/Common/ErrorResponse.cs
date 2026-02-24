namespace Maliev.CountryService.Api.Models.Common;

/// <summary>
/// Response model for error information.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public string ErrorCode { get; set; } = null!;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// Gets or sets the error details.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier for tracking the request.
    /// </summary>
    public string CorrelationId { get; set; } = null!;
}
