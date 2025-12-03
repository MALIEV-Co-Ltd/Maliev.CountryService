using Maliev.CountryService.Api.Models.Common;
using Maliev.CountryService.Api.Models.Countries;

namespace Maliev.CountryService.Api.Services;

public interface ICountryService
{
    Task<CountryResponse?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<CountryResponse?> GetByIso2Async(string iso2, CancellationToken cancellationToken = default);
    Task<CountryResponse?> GetByIso3Async(string iso3, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<CountryResponse>> ListAsync(CountryListRequest request, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<CountryResponse>> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default);
    Task InvalidateListCachesAsync(CancellationToken cancellationToken = default);

    // Admin operations
    Task<CountryResponse> CreateAsync(CreateCountryRequest request, string userId, CancellationToken cancellationToken = default);
    Task<CountryResponse> UpdateAsync(long id, UpdateCountryRequest request, string ifMatch, string userId, CancellationToken cancellationToken = default);
    Task<CountryResponse> PatchAsync(long id, PatchCountryRequest request, string ifMatch, string userId, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(long id, string userId, CancellationToken cancellationToken = default);
    Task HardDeleteAsync(long id, string userId, CancellationToken cancellationToken = default);
}