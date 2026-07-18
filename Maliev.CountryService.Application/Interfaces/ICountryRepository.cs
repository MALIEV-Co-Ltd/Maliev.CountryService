using Maliev.CountryService.Domain.Entities;

namespace Maliev.CountryService.Application.Interfaces;

/// <summary>
/// Repository interface for Country entity operations.
/// </summary>
public interface ICountryRepository
{
    /// <summary>
    /// Gets a country by its unique identifier.
    /// </summary>
    Task<Country?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a country by its ISO2 code.
    /// </summary>
    Task<Country?> GetByIso2Async(string iso2, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a country by its ISO3 code.
    /// </summary>
    Task<Country?> GetByIso3Async(string iso3, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active countries.
    /// </summary>
    Task<IReadOnlyList<Country>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches countries by query string with pagination.
    /// </summary>
    Task<IReadOnlyList<Country>> SearchAsync(string? query, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists countries with filtering and sorting.
    /// </summary>
    Task<IReadOnlyList<Country>> ListAsync(CountryListFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new country.
    /// </summary>
    Task<Country> AddAsync(Country country, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing country.
    /// </summary>
    Task UpdateAsync(Country country, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a country by its unique identifier.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a country exists with the given ISO2 code.
    /// </summary>
    Task<bool> ExistsByIso2Async(string iso2, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a country exists with the given ISO3 code.
    /// </summary>
    Task<bool> ExistsByIso3Async(string iso3, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a country exists with the given ISO2 code, excluding a specific ID.
    /// </summary>
    Task<bool> ExistsByIso2ExcludingIdAsync(string iso2, Guid excludeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a country exists with the given ISO3 code, excluding a specific ID.
    /// </summary>
    Task<bool> ExistsByIso3ExcludingIdAsync(string iso3, Guid excludeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts all active countries.
    /// </summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Filter options for listing countries.
/// </summary>
public class CountryListFilter
{
    /// <summary>
    /// Gets or sets the region filter.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the subregion filter.
    /// </summary>
    public string? Subregion { get; set; }

    /// <summary>
    /// Gets or sets the active status filter.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Gets or sets the sort field.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets the sort order (asc/desc).
    /// </summary>
    public string? SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 20;
}
