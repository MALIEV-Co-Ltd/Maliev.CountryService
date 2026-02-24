namespace Maliev.CountryService.Api.Services;

/// <summary>
/// T122: Tracks degraded mode state for the current request scope.
/// </summary>
public class DegradationContext
{
    /// <summary>
    /// Gets or sets a value indicating whether the service is operating in degraded mode (serving from cache only due to database unavailability).
    /// </summary>
    public bool IsDegraded { get; set; }

    /// <summary>
    /// Gets or sets the reason for degradation (e.g., "Database unavailable").
    /// </summary>
    public string? DegradationReason { get; set; }
}
