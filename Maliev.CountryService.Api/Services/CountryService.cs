using AutoMapper;
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
    private readonly IMapper _mapper;

    public CountryService(
        CountryDbContext context,
        IMemoryCache cache,
        IOptions<CacheOptions> cacheOptions,
        ILogger<CountryService> logger,
        IMapper mapper)
    {
        _context = context;
        _cache = cache;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<CountryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting country by ID: {CountryId}", id);
        
        var cacheKey = CacheKeys.CountryById(id);
        
        if (_cache.TryGetValue(cacheKey, out CountryDto? cachedCountry))
        {
            _logger.LogDebug("Country {CountryId} retrieved from cache", id);
            return cachedCountry;
        }

        _logger.LogDebug("Fetching country {CountryId} from database", id);
        var country = await _context.Countries
            .Include(c => c.CountryCodes)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (country == null)
        {
            _logger.LogDebug("Country with ID {CountryId} not found", id);
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
        _logger.LogDebug("Searching for countries with request: {@SearchRequest}", request);
        
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
            _logger.LogDebug("Applied name filter: {Name}", request.Name);
        }

        if (!string.IsNullOrWhiteSpace(request.Continent))
        {
            query = query.Where(c => c.Continent.Contains(request.Continent));
            _logger.LogDebug("Applied continent filter: {Continent}", request.Continent);
        }

        if (!string.IsNullOrWhiteSpace(request.ISO2))
        {
            query = query.Where(c => c.ISO2 == request.ISO2);
            _logger.LogDebug("Applied ISO2 filter: {ISO2}", request.ISO2);
        }

        if (!string.IsNullOrWhiteSpace(request.ISO3))
        {
            query = query.Where(c => c.ISO3 == request.ISO3);
            _logger.LogDebug("Applied ISO3 filter: {ISO3}", request.ISO3);
        }

        if (!string.IsNullOrWhiteSpace(request.CountryCode))
        {
            query = query.Where(c => c.CountryCodes.Any(cc => cc.Code == request.CountryCode));
            _logger.LogDebug("Applied country code filter: {CountryCode}", request.CountryCode);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        _logger.LogDebug("Found {Count} countries matching search criteria", totalCount);

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
            
            _logger.LogDebug("Applied sorting: {SortBy} {SortDirection}", request.SortBy, isAscending ? "ASC" : "DESC");
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
        _logger.LogDebug("Country search results cached for {Duration} minutes. Page {PageNumber} of {TotalPages} pages", _cacheOptions.SearchCacheDurationMinutes, request.PageNumber, Math.Ceiling((double)totalCount / request.PageSize));

        return result;
    }

    public async Task<PagedResult<CountryDto>> GetAllCountriesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all countries - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);
        
        // Validate parameters
        if (pageNumber < 1)
        {
            _logger.LogWarning("Invalid page number: {PageNumber}. Must be greater than 0", pageNumber);
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));
        }
        
        if (pageSize < 1 || pageSize > 1000)
        {
            _logger.LogWarning("Invalid page size: {PageSize}. Must be between 1 and 1000", pageSize);
            throw new ArgumentException("Page size must be between 1 and 1000", nameof(pageSize));
        }

        var cacheKey = $"country:all:{pageNumber}:{pageSize}";
        
        if (_cache.TryGetValue(cacheKey, out PagedResult<CountryDto>? cachedResult) && cachedResult != null)
        {
            _logger.LogDebug("All countries result retrieved from cache");
            return cachedResult;
        }

        // Get total count
        _logger.LogDebug("Fetching total count of countries from database");
        var totalCount = await _context.Countries.CountAsync(cancellationToken);
        _logger.LogDebug("Total countries in database: {TotalCount}", totalCount);

        // Get paged results
        _logger.LogDebug("Fetching page {PageNumber} of countries with page size {PageSize}", pageNumber, pageSize);
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
        _logger.LogDebug("All countries results cached for {Duration} minutes. Page {PageNumber} of {TotalPages} pages", _cacheOptions.SearchCacheDurationMinutes, pageNumber, Math.Ceiling((double)totalCount / pageSize));

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
            
            // Check if it's a concurrency conflict
            if (IsConcurrencyConflict(ex))
            {
                throw new ConcurrencyConflictException($"A concurrency conflict occurred while creating country '{country.Name}'. Please try again.", ex);
            }
            
            throw new DuplicateCountryException($"A country with the same name, ISO code, or country code already exists.");
        }
        catch (Exception ex) when (IsDatabaseUnavailable(ex))
        {
            _logger.LogError(ex, "Database unavailable when creating country: {CountryName}", country.Name);
            throw new DatabaseUnavailableException($"The database is currently unavailable. Please try again later.", ex);
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
            
            // Check if it's a concurrency conflict
            if (IsConcurrencyConflict(ex))
            {
                throw new ConcurrencyConflictException($"A concurrency conflict occurred while updating country '{country.Name}'. Please try again.", ex);
            }
            
            throw new DuplicateCountryException($"A country with the same name, ISO code, or country code already exists.");
        }
        catch (Exception ex) when (IsDatabaseUnavailable(ex))
        {
            _logger.LogError(ex, "Database unavailable when updating country: {CountryName}", country.Name);
            throw new DatabaseUnavailableException($"The database is currently unavailable. Please try again later.", ex);
        }

        InvalidateCache();
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
            
            // Check if it's a concurrency conflict
            if (IsConcurrencyConflict(ex))
            {
                throw new ConcurrencyConflictException($"A concurrency conflict occurred while deleting country '{country.Name}'. Please try again.", ex);
            }
            
            throw new CountryServiceException($"An error occurred while deleting the country.");
        }
        catch (Exception ex) when (IsDatabaseUnavailable(ex))
        {
            _logger.LogError(ex, "Database unavailable when deleting country: {CountryName}", country.Name);
            throw new DatabaseUnavailableException($"The database is currently unavailable. Please try again later.", ex);
        }

        InvalidateCache();
        _logger.LogInformation("Country deleted: {CountryName} with ID {CountryId}", country.Name, country.Id);

        return true;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if country with ID {CountryId} exists", id);
        var exists = await _context.Countries.AnyAsync(c => c.Id == id, cancellationToken);
        _logger.LogDebug("Country with ID {CountryId} exists: {Exists}", id, exists);
        return exists;
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if country with name '{CountryName}' exists (exclude ID: {ExcludeId})", name, excludeId);
        var query = _context.Countries.Where(c => c.Name == name);
        
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        
        var exists = await query.AnyAsync(cancellationToken);
        _logger.LogDebug("Country with name '{CountryName}' exists: {Exists}", name, exists);
        return exists;
    }

    public async Task<bool> ExistsByIso2Async(string iso2, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if country with ISO2 '{ISO2}' exists (exclude ID: {ExcludeId})", iso2, excludeId);
        var query = _context.Countries.Where(c => c.ISO2 == iso2);
        
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        
        var exists = await query.AnyAsync(cancellationToken);
        _logger.LogDebug("Country with ISO2 '{ISO2}' exists: {Exists}", iso2, exists);
        return exists;
    }

    public async Task<bool> ExistsByIso3Async(string iso3, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if country with ISO3 '{ISO3}' exists (exclude ID: {ExcludeId})", iso3, excludeId);
        var query = _context.Countries.Where(c => c.ISO3 == iso3);
        
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        
        var exists = await query.AnyAsync(cancellationToken);
        _logger.LogDebug("Country with ISO3 '{ISO3}' exists: {Exists}", iso3, exists);
        return exists;
    }

    public async Task<bool> ExistsByCountryCodeAsync(string countryCode, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if country with country code '{CountryCode}' exists (exclude ID: {ExcludeId})", countryCode, excludeId);
        var query = _context.CountryCodes.Where(cc => cc.Code == countryCode);
        
        if (excludeId.HasValue)
        {
            query = query.Where(cc => cc.CountryId != excludeId.Value);
        }
        
        var exists = await query.AnyAsync(cancellationToken);
        _logger.LogDebug("Country with country code '{CountryCode}' exists: {Exists}", countryCode, exists);
        return exists;
    }

    public async Task<IEnumerable<string>> GetContinentsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting list of continents");
        
        if (_cache.TryGetValue(CacheKeys.Continents, out IEnumerable<string>? cachedContinents) && cachedContinents != null)
        {
            _logger.LogDebug("Continents retrieved from cache");
            return cachedContinents;
        }

        _logger.LogDebug("Fetching continents from database");
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
        _logger.LogDebug("Continents cached for {Duration} minutes. Found {Count} continents", _cacheOptions.CountryCacheDurationMinutes, continents.Count());

        return continents;
    }

    private CountryDto MapToDto(Country country)
    {
        return _mapper.Map<CountryDto>(country);
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
    
    private bool IsConcurrencyConflict(DbUpdateException ex)
    {
        // Check if it's a concurrency conflict by looking at the inner exception
        // This is a simplified check - in a real-world scenario, you'd need to check
        // the specific database provider's exception types and error codes
        return ex.InnerException?.Message.Contains("concurrency", StringComparison.OrdinalIgnoreCase) == true ||
               ex.InnerException?.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase) == true ||
               ex.Message.Contains("concurrency", StringComparison.OrdinalIgnoreCase) == true ||
               ex.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase) == true;
    }
    
    private bool IsDatabaseUnavailable(Exception ex)
    {
        // Check if it's a database unavailable error
        return ex is InvalidOperationException && 
               (ex.Message.Contains("database", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase));
    }
}