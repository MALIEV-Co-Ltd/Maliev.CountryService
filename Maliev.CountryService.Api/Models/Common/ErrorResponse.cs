namespace Maliev.CountryService.Api.Models.Common;

public class ErrorResponse
{
    public string ErrorCode { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? Details { get; set; }
    public string CorrelationId { get; set; } = null!;
}
