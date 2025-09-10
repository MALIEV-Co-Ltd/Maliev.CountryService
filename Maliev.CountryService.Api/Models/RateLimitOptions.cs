using System.ComponentModel.DataAnnotations;

namespace Maliev.CountryService.Api.Models;

public class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    [Required]
    public GlobalRateLimit Global { get; set; } = new();

    [Required]
    public CountryEndpointRateLimit CountryEndpoint { get; set; } = new();
}

public class GlobalRateLimit
{
    [Required]
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 1000;

    [Required]
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    [Required]
    [Range(0, int.MaxValue)]
    public int QueueLimit { get; set; } = 100;
}

public class CountryEndpointRateLimit
{
    [Required]
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 100;

    [Required]
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    [Required]
    [Range(0, int.MaxValue)]
    public int QueueLimit { get; set; } = 50;
}