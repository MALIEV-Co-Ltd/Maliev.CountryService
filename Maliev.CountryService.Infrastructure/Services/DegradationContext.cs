using Maliev.CountryService.Application.Interfaces;

namespace Maliev.CountryService.Infrastructure.Services;

/// <summary>
/// Tracks degraded mode state for the current request scope.
/// When the database is unavailable, the service operates in degraded mode,
/// serving responses from cache only.
/// </summary>
public class DegradationContext : IDegradationContext
{
    /// <inheritdoc />
    public bool IsDegraded { get; set; }

    /// <inheritdoc />
    public string? DegradationReason { get; set; }
}
