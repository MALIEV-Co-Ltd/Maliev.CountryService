using System.ComponentModel.DataAnnotations;

namespace Maliev.CountryService.Api.Models;

public class CacheOptions
{
    public const string SectionName = "Cache";

    [Required]
    [Range(1, int.MaxValue)]
    public int CountryCacheDurationMinutes { get; set; } = 60;

    [Required]
    [Range(100, int.MaxValue)]
    public int MaxCacheSize { get; set; } = 1000;

    [Required]
    [Range(1, int.MaxValue)]
    public int SearchCacheDurationMinutes { get; set; } = 30;
}