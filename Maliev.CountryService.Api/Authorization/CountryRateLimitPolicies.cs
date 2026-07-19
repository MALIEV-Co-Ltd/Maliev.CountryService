namespace Maliev.CountryService.Api.Authorization;

/// <summary>
/// Rate-limit policies consumed by CountryService administrative endpoints.
/// </summary>
public static class CountryRateLimitPolicies
{
    /// <summary>
    /// Uses the shared stricter limiter for administrative mutations.
    /// </summary>
    public const string Admin = "write";

    /// <summary>
    /// Uses the shared stricter limiter for bulk-import operations.
    /// </summary>
    public const string Batch = "write";
}
