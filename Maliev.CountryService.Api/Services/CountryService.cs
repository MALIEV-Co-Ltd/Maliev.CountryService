using Maliev.CountryService.Api.Exceptions;
using Maliev.CountryService.Api.Models;
using Maliev.CountryService.Data.DbContexts;
using Maliev.CountryService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Maliev.CountryService.Api.Services;

public class CountryService : ICountryService
{
    private readonly CountryDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly CacheOptions _cacheOptions;
    private readonly ILogger<CountryService> _logger;

    public CountryService(
        CountryDbContext context,
        IMemoryCache cache,
        IOptions<CacheOptions> cacheOptions,
        ILogger<CountryService> logger)
    {
        _context = context;
        _cache = cache;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
    }

    public async Task<CountryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.CountryById(id);
        
        if (_cache.TryGetValue(cacheKey, out CountryDto? cachedCountry))
        {
            _logger.LogDebug("Country {CountryId} retrieved from cache", id);
            return cachedCountry;
        }

        var country = await _context.Countries
            .Include(c => c.CountryCodes)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (country == null)
        {
            return null;
        }

        var dto = MapToDto(country);
        
        _cache.Set(cacheKey, dto, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.CountryCacheDurationMinutes),
            Size = 1 // Each country entry counts as 1 unit
        });
        _logger.LogDebug("Country {CountryId} cached for {Duration} minutes", id, _cacheOptions.CountryCacheDurationMinutes);

        return dto;
    }

    public async Task<PagedResult<CountryDto>> SearchAsync(CountrySearchRequest request, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.CountrySearch(
            request.Name, 
            request.Continent, 
            request.ISO2, 
            request.ISO3, 
            request.CountryCode, 
            request.PageNumber, 
            request.PageSize, 
            request.SortBy, 
            request.SortDirection);
        
        if (_cache.TryGetValue(cacheKey, out PagedResult<CountryDto>? cachedResult) && cachedResult != null)
        {
            _logger.LogDebug("Country search result retrieved from cache");
            return cachedResult;
        }

        var query = _context.Countries
            .Include(c => c.CountryCodes)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            query = query.Where(c => c.Name.Contains(request.Name));
        }

        if (!string.IsNullOrWhiteSpace(request.Continent))
        {
            query = query.Where(c => c.Continent.Contains(request.Continent));
        }

        if (!string.IsNullOrWhiteSpace(request.ISO2))
        {
            query = query.Where(c => c.ISO2 == request.ISO2);
        }

        if (!string.IsNullOrWhiteSpace(request.ISO3))
        {
            query = query.Where(c => c.ISO3 == request.ISO3);
        }

        if (!string.IsNullOrWhiteSpace(request.CountryCode))
        {
            query = query.Where(c => c.CountryCodes.Any(cc => cc.Code == request.CountryCode));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            var isAscending = string.IsNullOrWhiteSpace(request.SortDirection) || 
                            request.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);

            query = request.SortBy.ToLower() switch
            {
                "name" => isAscending ? query.OrderBy(c => c.Name) : query.OrderByDescending(c => c.Name),
                "continent" => isAscending ? query.OrderBy(c => c.Continent) : query.OrderByDescending(c => c.Continent),
                "countrycode" => isAscending ? query.OrderBy(c => c.CountryCodes.FirstOrDefault(cc => cc.IsPrimary)!.Code) : query.OrderByDescending(c => c.CountryCodes.FirstOrDefault(cc => cc.IsPrimary)!.Code),
                "iso2" => isAscending ? query.OrderBy(c => c.ISO2) : query.OrderByDescending(c => c.ISO2),
                "iso3" => isAscending ? query.OrderBy(c => c.ISO3) : query.OrderByDescending(c => c.ISO3),
                "createddate" => isAscending ? query.OrderBy(c => c.CreatedDate) : query.OrderByDescending(c => c.CreatedDate),
                "modifieddate" => isAscending ? query.OrderBy(c => c.ModifiedDate) : query.OrderByDescending(c => c.ModifiedDate),
                _ => query.OrderBy(c => c.Name)
            };
        }

        var countries = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<CountryDto>
        {
            Items = countries.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.SearchCacheDurationMinutes),
            Size = result.Items.Count() // Size based on number of items in result
        });
        _logger.LogDebug("Country search results cached for {Duration} minutes", _cacheOptions.SearchCacheDurationMinutes);

        return result;
    }

    public async Task<PagedResult<CountryDto>> GetAllCountriesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        // Validate parameters
        if (pageNumber < 1)
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));
        
        if (pageSize < 1 || pageSize > 1000)
            throw new ArgumentException("Page size must be between 1 and 1000", nameof(pageSize));

        var cacheKey = $"country:all:{pageNumber}:{pageSize}";
        
        if (_cache.TryGetValue(cacheKey, out PagedResult<CountryDto>? cachedResult) && cachedResult != null)
        {
            _logger.LogDebug("All countries result retrieved from cache");
            return cachedResult;
        }

        // Get total count
        var totalCount = await _context.Countries.CountAsync(cancellationToken);

        // Get paged results
        var countries = await _context.Countries
            .Include(c => c.CountryCodes)
            .AsNoTracking()
            .OrderBy(c => c.Name) // Default sort by name
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<CountryDto>
        {
            Items = countries.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.SearchCacheDurationMinutes),
            Size = result.Items.Count() // Size based on number of items in result
        });
        _logger.LogDebug("All countries results cached for {Duration} minutes", _cacheOptions.SearchCacheDurationMinutes);

        return result;
    }

    public async Task<CountryDto> CreateAsync(CreateCountryRequest request, CancellationToken cancellationToken = default)
    {
        var country = new Country
        {
            Name = request.Name.Trim(),
            Continent = request.Continent.Trim(),
            ISO2 = request.ISO2.ToUpperInvariant(),
            ISO3 = request.ISO3.ToUpperInvariant()
        };

        // Create country code(s) - for now, assume single code, but ready for multiple
        var codes = request.CountryCode.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(code => code.Trim())
            .ToList();

        for (int i = 0; i < codes.Count; i++)
        {
            var countryCode = new Data.Entities.CountryCode
            {
                Code = codes[i],
                IsPrimary = i == 0, // First code is primary
                Country = country
            };
            country.CountryCodes.Add(countryCode);
        }

        _context.Countries.Add(country);
        
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database constraint violation when creating country: {CountryName}", country.Name);
            throw new DuplicateCountryException($"A country with the same name, ISO code, or country code already exists.");
        }

        InvalidateCache();
        _logger.LogInformation("Country created: {CountryName} with ID {CountryId}", country.Name, country.Id);

        return MapToDto(country);
    }

    public async Task<CountryDto?> UpdateAsync(int id, UpdateCountryRequest request, CancellationToken cancellationToken = default)
    {
        var country = await _context.Countries
            .Include(c => c.CountryCodes)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        
        if (country == null)
        {
            return null;
        }

        country.Name = request.Name.Trim();
        country.Continent = request.Continent.Trim();
        country.ISO2 = request.ISO2.ToUpperInvariant();
        country.ISO3 = request.ISO3.ToUpperInvariant();

        // Update country codes
        var newCodes = request.CountryCode.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(code => code.Trim())
            .ToList();

        // Remove existing codes
        _context.CountryCodes.RemoveRange(country.CountryCodes);
        country.CountryCodes.Clear();

        // Add new codes
        for (int i = 0; i < newCodes.Count; i++)
        {
            var countryCode = new Data.Entities.CountryCode
            {
                Code = newCodes[i],
                IsPrimary = i == 0, // First code is primary
                CountryId = country.Id
            };
            country.CountryCodes.Add(countryCode);
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database constraint violation when updating country: {CountryName}", country.Name);
            throw new DuplicateCountryException($"A country with the same name, ISO code, or country code already exists.");
        }

        InvalidateCountryCache(country.Id);
        _logger.LogInformation("Country updated: {CountryName} with ID {CountryId}", country.Name, country.Id);

        return MapToDto(country);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var country = await _context.Countries.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        
        if (country == null)
        {
            return false;
        }

        _context.Countries.Remove(country);
        
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error when deleting country: {CountryName}", country.Name);
            throw new CountryServiceException($"An error occurred while deleting the country.");
        }

        InvalidateCountryCache(country.Id);
        _logger.LogInformation("Country deleted: {CountryName} with ID {CountryId}", country.Name, country.Id);

        return true;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Countries.AnyAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Countries.Where(c => c.Name == name);
        
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsByIso2Async(string iso2, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Countries.Where(c => c.ISO2 == iso2);
        
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsByIso3Async(string iso3, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Countries.Where(c => c.ISO3 == iso3);
        
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCountryCodeAsync(string countryCode, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.CountryCodes.Where(cc => cc.Code == countryCode);
        
        if (excludeId.HasValue)
        {
            query = query.Where(cc => cc.CountryId != excludeId.Value);
        }
        
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetContinentsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKeys.Continents, out IEnumerable<string>? cachedContinents) && cachedContinents != null)
        {
            _logger.LogDebug("Continents retrieved from cache");
            return cachedContinents;
        }

        var continents = await _context.Countries
            .AsNoTracking()
            .Select(c => c.Continent)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);

        _cache.Set(CacheKeys.Continents, continents, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.CountryCacheDurationMinutes),
            Size = continents.Count() // Size based on number of continents
        });
        _logger.LogDebug("Continents cached for {Duration} minutes", _cacheOptions.CountryCacheDurationMinutes);

        return continents;
    }

    private static CountryDto MapToDto(Country country)
    {
        return new CountryDto
        {
            Id = country.Id,
            Name = country.Name,
            Continent = country.Continent,
            CountryCodes = country.CountryCodes.Select(cc => new CountryCodeDto
            {
                Id = cc.Id,
                Code = cc.Code,
                IsPrimary = cc.IsPrimary
            }).ToList(),
            ISO2 = country.ISO2,
            ISO3 = country.ISO3,
            CreatedDate = country.CreatedDate,
            ModifiedDate = country.ModifiedDate
        };
    }

    private void InvalidateCache()
    {
        // Remove all country-related cache entries
        _logger.LogDebug("Invalidating country cache");
        
        // Remove continents cache
        _cache.Remove(CacheKeys.Continents);
        
        // Note: For a more sophisticated cache invalidation strategy,
        // we would need to track individual cache keys or use cache tagging.
        // For now, we're invalidating the most critical cache entry (continents).
        // In a production environment with Redis, we could use pattern-based deletion.
    }
    
    private void InvalidateCountryCache(int countryId)
    {
        // Remove specific country cache
        _cache.Remove(CacheKeys.CountryById(countryId));
        
        // Also invalidate the continents cache as it might be affected
        _cache.Remove(CacheKeys.Continents);
        
        // Note: We're not invalidating search caches here because it would require
        // tracking all possible search parameter combinations, which is complex.
        // In a production environment with Redis, we could use pattern-based deletion
        // to remove all search cache entries.
    }
}