using Maliev.CountryService.Api.Models;

namespace Maliev.CountryService.Api.Services;

public interface ICountryService
{
    Task<CountryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<CountryDto>> SearchAsync(CountrySearchRequest request, CancellationToken cancellationToken = default);
    Task<CountryDto> CreateAsync(CreateCountryRequest request, CancellationToken cancellationToken = default);
    Task<CountryDto?> UpdateAsync(int id, UpdateCountryRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIso2Async(string iso2, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIso3Async(string iso3, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCountryCodeAsync(string countryCode, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetContinentsAsync(CancellationToken cancellationToken = default);
}