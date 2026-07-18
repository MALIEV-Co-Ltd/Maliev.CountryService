using Maliev.CountryService.Application.Interfaces;
using Maliev.CountryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CountryService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementation of ICountryRepository using Entity Framework Core.
/// </summary>
public class CountryRepository : ICountryRepository
{
    private readonly CountryDbContext _context;

    /// <summary>
    /// Initializes a new instance of the CountryRepository class.
    /// </summary>
    public CountryRepository(CountryDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<Country?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Country?> GetByIso2Async(string iso2, CancellationToken cancellationToken = default)
    {
        return await _context.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Iso2 == iso2.ToUpperInvariant(), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Country?> GetByIso3Async(string iso3, CancellationToken cancellationToken = default)
    {
        return await _context.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Iso3 == iso3.ToUpperInvariant(), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Country>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Countries
            .AsNoTracking()
            .Where(c => c.IsActive)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Country>> SearchAsync(string? query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var queryable = _context.Countries.AsNoTracking().Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lowerQuery = query.ToLowerInvariant();
            queryable = queryable.Where(c =>
                c.Name.ToLower().Contains(lowerQuery) ||
                (c.OfficialName != null && c.OfficialName.ToLower().Contains(lowerQuery)) ||
                c.Iso2.ToLower() == lowerQuery ||
                c.Iso3!.ToLower() == lowerQuery);
        }

        return await queryable
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Country>> ListAsync(CountryListFilter filter, CancellationToken cancellationToken = default)
    {
        var queryable = _context.Countries.AsNoTracking().Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.Region))
            queryable = queryable.Where(c => c.Region == filter.Region);

        if (!string.IsNullOrWhiteSpace(filter.Subregion))
            queryable = queryable.Where(c => c.Subregion == filter.Subregion);

        queryable = filter.SortBy?.ToLower() switch
        {
            "name" => filter.SortOrder?.ToLower() == "desc"
                ? queryable.OrderByDescending(c => c.Name)
                : queryable.OrderBy(c => c.Name),
            "population" => filter.SortOrder?.ToLower() == "desc"
                ? queryable.OrderByDescending(c => c.Population)
                : queryable.OrderBy(c => c.Population),
            "area" => filter.SortOrder?.ToLower() == "desc"
                ? queryable.OrderByDescending(c => c.AreaKm2)
                : queryable.OrderBy(c => c.AreaKm2),
            "iso2" => filter.SortOrder?.ToLower() == "desc"
                ? queryable.OrderByDescending(c => c.Iso2)
                : queryable.OrderBy(c => c.Iso2),
            _ => queryable.OrderBy(c => c.Name)
        };

        return await queryable
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Country> AddAsync(Country country, CancellationToken cancellationToken = default)
    {
        _context.Countries.Add(country);
        await _context.SaveChangesAsync(cancellationToken);
        return country;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Country country, CancellationToken cancellationToken = default)
    {
        _context.Countries.Update(country);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var country = await _context.Countries.FindAsync(new object[] { id }, cancellationToken);
        if (country != null)
        {
            _context.Countries.Remove(country);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByIso2Async(string iso2, CancellationToken cancellationToken = default)
    {
        return await _context.Countries.AnyAsync(c => c.Iso2 == iso2.ToUpperInvariant(), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByIso3Async(string iso3, CancellationToken cancellationToken = default)
    {
        return await _context.Countries.AnyAsync(c => c.Iso3 == iso3.ToUpperInvariant(), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByIso2ExcludingIdAsync(string iso2, Guid excludeId, CancellationToken cancellationToken = default)
    {
        return await _context.Countries.AnyAsync(c => c.Iso2 == iso2.ToUpperInvariant() && c.Id != excludeId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByIso3ExcludingIdAsync(string iso3, Guid excludeId, CancellationToken cancellationToken = default)
    {
        return await _context.Countries.AnyAsync(c => c.Iso3 == iso3.ToUpperInvariant() && c.Id != excludeId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Countries.CountAsync(c => c.IsActive, cancellationToken);
    }
}
