using Maliev.CountryService.Api.Models.Common;
using Maliev.CountryService.Api.Models.Countries;

namespace Maliev.CountryService.Api.Services;

/// <summary>
/// Represents a service for managing country data.
/// </summary>
public interface ICountryService
{
    /// <summary>
    /// Retrieves a country by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the country.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The country response, or null if not found.</returns>
    Task<CountryResponse?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves a country by its ISO2 code.
    /// </summary>
    /// <param name="iso2">The ISO2 code of the country.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The country response, or null if not found.</returns>
    Task<CountryResponse?> GetByIso2Async(string iso2, CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves a country by its ISO3 code.
    /// </summary>
    /// <param name="iso3">The ISO3 code of the country.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The country response, or null if not found.</returns>
    Task<CountryResponse?> GetByIso3Async(string iso3, CancellationToken cancellationToken = default);
    /// <summary>
    /// Lists countries based on a provided request.
    /// </summary>
    /// <param name="request">The country list request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated response of countries.</returns>
    Task<PaginatedResponse<CountryResponse>> ListAsync(CountryListRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Searches for countries based on a query string.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated response of countries matching the search query.</returns>
    Task<PaginatedResponse<CountryResponse>> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default);
    /// <summary>
    /// Invalidates all country list related caches.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task InvalidateListCachesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new country. (Admin operation)
    /// </summary>
    /// <param name="request">The request to create a country.</param>
    /// <param name="userId">The ID of the user performing the operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created country response.</returns>
    Task<CountryResponse> CreateAsync(CreateCountryRequest request, string userId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Updates an existing country. (Admin operation)
    /// </summary>
    /// <param name="id">The ID of the country to update.</param>
    /// <param name="request">The request to update the country.</param>
    /// <param name="ifMatch">The If-Match header value for optimistic concurrency.</param>
    /// <param name="userId">The ID of the user performing the operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated country response.</returns>
    Task<CountryResponse> UpdateAsync(long id, UpdateCountryRequest request, string ifMatch, string userId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Partially updates an existing country. (Admin operation)
    /// </summary>
    /// <param name="id">The ID of the country to patch.</param>
    /// <param name="request">The request to patch the country.</param>
    /// <param name="ifMatch">The If-Match header value for optimistic concurrency.</param>
    /// <param name="userId">The ID of the user performing the operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The patched country response.</returns>
    Task<CountryResponse> PatchAsync(long id, PatchCountryRequest request, string ifMatch, string userId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Soft deletes a country, marking it as deleted without removing it from the database. (Admin operation)
    /// </summary>
    /// <param name="id">The ID of the country to soft delete.</param>
    /// <param name="userId">The ID of the user performing the operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SoftDeleteAsync(long id, string userId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Hard deletes a country, permanently removing it from the database. (Admin operation)
    /// </summary>
    /// <param name="id">The ID of the country to hard delete.</param>
    /// <param name="userId">The ID of the user performing the operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task HardDeleteAsync(long id, string userId, CancellationToken cancellationToken = default);
}