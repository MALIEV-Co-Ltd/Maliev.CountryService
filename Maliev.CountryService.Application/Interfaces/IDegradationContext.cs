namespace Maliev.CountryService.Application.Interfaces;

/// <summary>
/// Tracks degraded mode state for the current request scope.
/// </summary>
public interface IDegradationContext
{
    /// <summary>
    /// Gets or sets a value indicating whether the service is operating in degraded mode
    /// (serving from cache only due to database unavailability).
    /// </summary>
    bool IsDegraded { get; set; }

    /// <summary>
    /// Gets or sets the reason for degradation (e.g., "Database unavailable").
    /// </summary>
    string? DegradationReason { get; set; }
}
