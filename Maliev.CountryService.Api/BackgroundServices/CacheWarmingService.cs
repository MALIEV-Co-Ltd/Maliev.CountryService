using Maliev.CountryService.Api.Services;
using System.Text.Json;

namespace Maliev.CountryService.Api.BackgroundServices;

/// <summary>
/// T060: Background service that loads Top50PopulousCountries.json and pre-caches on startup with 5-second delay.
/// </summary>
public class CacheWarmingService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CacheWarmingService> _logger;

    public CacheWarmingService(IServiceScopeFactory scopeFactory, ILogger<CacheWarmingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cache warming service starting - waiting 5 seconds before warming cache");
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var countryService = scope.ServiceProvider.GetRequiredService<ICountryService>();

            // Load top 50 ISO2 codes from configuration file
            var iso2Codes = await LoadTop50CountryCodesAsync(cancellationToken);
            
            _logger.LogInformation("Warming cache for {Count} top countries", iso2Codes.Count);

            var tasks = iso2Codes.Select(async iso2 =>
            {
                try
                {
                    var country = await countryService.GetByIso2Async(iso2, cancellationToken);
                    if (country != null)
                    {
                        _logger.LogDebug("Pre-cached country: {Iso2} - {Name}", iso2, country.Name);
                    }
                    else
                    {
                        _logger.LogWarning("Country not found for ISO2: {Iso2}", iso2);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to pre-cache country {Iso2}", iso2);
                }
            });

            await Task.WhenAll(tasks);
            _logger.LogInformation("Cache warming completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache warming failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cache warming service stopping");
        return Task.CompletedTask;
    }

    private async Task<List<string>> LoadTop50CountryCodesAsync(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "Configuration", "Top50PopulousCountries.json");
        
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Top50PopulousCountries.json not found at {FilePath}", filePath);
            return new List<string>();
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var codes = JsonSerializer.Deserialize<List<string>>(json);
        
        return codes ?? new List<string>();
    }
}
