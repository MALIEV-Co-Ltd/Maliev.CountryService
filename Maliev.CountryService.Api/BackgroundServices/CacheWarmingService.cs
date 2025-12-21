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
    /// <summary>
    /// Initializes a new instance of the <see cref="CacheWarmingService"/> class.
    /// </summary>
    /// <param name="scopeFactory">The service scope factory.</param>
    /// <param name="logger">The logger instance.</param>
    public CacheWarmingService(IServiceScopeFactory scopeFactory, ILogger<CacheWarmingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    /// <summary>
    /// Starts the cache warming service asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cache warming service starting - waiting 5 seconds before warming cache");
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

        try
        {
            // Load top 50 ISO2 codes from configuration file
            var iso2Codes = await LoadTop50CountryCodesAsync(cancellationToken);
            
            _logger.LogInformation("Warming cache for {Count} top countries (Thailand prioritized)", iso2Codes.Count);

            // Prioritize Thailand (TH) - cache it first since the app is served in Thailand region
            if (iso2Codes.Contains("TH"))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var countryService = scope.ServiceProvider.GetRequiredService<ICountryService>();
                    
                    var thailand = await countryService.GetByIso2Async("TH", cancellationToken);
                    if (thailand != null)
                    {
                        _logger.LogInformation("Pre-cached PRIORITY country: TH - {Name}", thailand.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to pre-cache priority country TH");
                }
            }

            // Cache remaining countries in parallel (excluding TH since it's already cached)
            var remainingCodes = iso2Codes.Where(code => code != "TH").ToList();
            
            // Each parallel task needs its own scope to avoid DbContext threading issues
            var tasks = remainingCodes.Select(async iso2 =>
            {
                try
                {
                    // Create a new scope for each country to ensure thread-safe DbContext usage
                    using var scope = _scopeFactory.CreateScope();
                    var countryService = scope.ServiceProvider.GetRequiredService<ICountryService>();
                    
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
    /// <summary>
    /// Stops the cache warming service asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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
