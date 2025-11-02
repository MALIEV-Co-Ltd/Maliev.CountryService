using Maliev.CountryService.Api.Models.Common;
using Maliev.CountryService.Api.Models.Countries;

namespace Maliev.CountryService.Api.Services;

/// <summary>
/// Service interface for country data operations.
/// </summary>
public interface ICountryService
{
    Task<CountryResponse?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<CountryResponse?> GetByIso2Async(string iso2, CancellationToken cancellationToken = default);
    Task<CountryResponse?> GetByIso3Async(string iso3, CancellationToken cancellationToken = default);

    /// <summary>
    /// T069: List countries with pagination, filtering, and sorting.
    /// </summary>
    Task<PaginatedResponse<CountryResponse>> ListAsync(CountryListRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// T071: Search countries by name using PostgreSQL full-text search.
    /// </summary>
    Task<PaginatedResponse<CountryResponse>> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// T070: Invalidate list caches (called when country data changes).
    /// </summary>
    Task InvalidateListCachesAsync(CancellationToken cancellationToken = default);

    // User Story 3: Admin CRUD operations

    /// <summary>
    /// T079-T080: Create a new country with validation and audit logging.
    /// </summary>
    Task<CountryResponse> CreateAsync(CreateCountryRequest request, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// T081-T082: Update an existing country (full replacement) with optimistic concurrency check.
    /// </summary>
    Task<CountryResponse> UpdateAsync(long id, UpdateCountryRequest request, string ifMatch, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// T083-T084: Partially update an existing country.
    /// </summary>
    Task<CountryResponse> PatchAsync(long id, PatchCountryRequest request, string ifMatch, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// T085-T086: Soft delete a country (set IsActive=false).
    /// </summary>
    Task SoftDeleteAsync(long id, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// T087-T088: Permanently delete a country (SuperAdmin only).
    /// </summary>
    Task HardDeleteAsync(long id, string userId, CancellationToken cancellationToken = default);
}
