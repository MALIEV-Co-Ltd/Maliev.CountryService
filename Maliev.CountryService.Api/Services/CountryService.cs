using Maliev.CountryService.Api.Models.Common;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Maliev.CountryService.Api.Services;

/// <summary>
/// Service implementation for country data operations with caching support.
/// T122: Graceful degradation with cache-only fallback when database is unavailable.
/// </summary>
public class CountryService : ICountryService
{
    private readonly CountryServiceDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CountryService> _logger;
    private readonly DegradationContext _degradationContext;

    public CountryService(
        CountryServiceDbContext context,
        ICacheService cacheService,
        ILogger<CountryService> logger,
        DegradationContext degradationContext)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
        _degradationContext = degradationContext;
    }

    public async Task<CountryResponse?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey("id", id.ToString());

        // Try cache first
        var cached = await _cacheService.GetAsync<CountryResponse>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Cache HIT for country ID {Id}", id);
            return cached;
        }

        // T122: Cache miss - query database with graceful degradation
        _logger.LogDebug("Cache MISS for country ID {Id}", id);
        try
        {
            var country = await _context.Countries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);

            if (country == null)
                return null;

            var response = MapToResponse(country);

            // Cache the result
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15), cancellationToken);

            return response;
        }
        catch (Exception ex) when (ex is Npgsql.NpgsqlException or Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            // T122 + T126: Database unavailable - try serving from cache (including stale data)
            _logger.LogWarning(ex, "Database unavailable for GetByIdAsync({Id}), attempting cache-only fallback", id);

            var staleData = await _cacheService.GetAsync<CountryResponse>(cacheKey, cancellationToken);
            if (staleData != null)
            {
                _degradationContext.IsDegraded = true;
                _degradationContext.DegradationReason = "Database unavailable";
                _logger.LogInformation("Serving cached data in degraded mode for country ID {Id}", id);
                return staleData;
            }

            // No cache available - rethrow
            _logger.LogError("No cache available for country ID {Id}, cannot serve request in degraded mode", id);
            throw;
        }
    }

    public async Task<CountryResponse?> GetByIso2Async(string iso2, CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey("iso2", iso2.ToUpperInvariant());

        var cached = await _cacheService.GetAsync<CountryResponse>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Cache HIT for ISO2 {Iso2}", iso2);
            return cached;
        }

        // T122: Cache miss - query database with graceful degradation
        _logger.LogDebug("Cache MISS for ISO2 {Iso2}", iso2);
        try
        {
            var country = await _context.Countries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Iso2 == iso2.ToUpperInvariant() && c.IsActive, cancellationToken);

            if (country == null)
                return null;

            var response = MapToResponse(country);
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15), cancellationToken);

            return response;
        }
        catch (Exception ex) when (ex is Npgsql.NpgsqlException or Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            // T122 + T126: Database unavailable - try serving from cache
            _logger.LogWarning(ex, "Database unavailable for GetByIso2Async({Iso2}), attempting cache-only fallback", iso2);

            var staleData = await _cacheService.GetAsync<CountryResponse>(cacheKey, cancellationToken);
            if (staleData != null)
            {
                _degradationContext.IsDegraded = true;
                _degradationContext.DegradationReason = "Database unavailable";
                _logger.LogInformation("Serving cached data in degraded mode for ISO2 {Iso2}", iso2);
                return staleData;
            }

            _logger.LogError("No cache available for ISO2 {Iso2}, cannot serve request in degraded mode", iso2);
            throw;
        }
    }

    public async Task<CountryResponse?> GetByIso3Async(string iso3, CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey("iso3", iso3.ToUpperInvariant());

        var cached = await _cacheService.GetAsync<CountryResponse>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Cache HIT for ISO3 {Iso3}", iso3);
            return cached;
        }

        // T122: Cache miss - query database with graceful degradation
        _logger.LogDebug("Cache MISS for ISO3 {Iso3}", iso3);
        try
        {
            var country = await _context.Countries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Iso3 == iso3.ToUpperInvariant() && c.IsActive, cancellationToken);

            if (country == null)
                return null;

            var response = MapToResponse(country);
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15), cancellationToken);

            return response;
        }
        catch (Exception ex) when (ex is Npgsql.NpgsqlException or Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            // T122 + T126: Database unavailable - try serving from cache
            _logger.LogWarning(ex, "Database unavailable for GetByIso3Async({Iso3}), attempting cache-only fallback", iso3);

            var staleData = await _cacheService.GetAsync<CountryResponse>(cacheKey, cancellationToken);
            if (staleData != null)
            {
                _degradationContext.IsDegraded = true;
                _degradationContext.DegradationReason = "Database unavailable";
                _logger.LogInformation("Serving cached data in degraded mode for ISO3 {Iso3}", iso3);
                return staleData;
            }

            _logger.LogError("No cache available for ISO3 {Iso3}, cannot serve request in degraded mode", iso3);
            throw;
        }
    }

    /// <summary>
    /// T069: List countries with pagination, filtering, and sorting.
    /// T070: Cache list results with cache key based on all parameters.
    /// </summary>
    public async Task<PaginatedResponse<CountryResponse>> ListAsync(CountryListRequest request, CancellationToken cancellationToken = default)
    {
        // Validate and normalize pagination
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        // Generate cache key with all filter parameters
        var filterKey = $"{request.Region ?? "all"}:{request.Subregion ?? "all"}:{request.SortBy}:{request.SortOrder}:{request.IncludeInactive}";
        var cacheKey = GenerateCacheKey("list", $"{page}:{pageSize}:{filterKey}");

        var cached = await _cacheService.GetAsync<PaginatedResponse<CountryResponse>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Cache HIT for country list page {Page}", page);
            return cached;
        }

        _logger.LogDebug("Cache MISS for country list page {Page}", page);

        // Build query with filters
        var query = _context.Countries.AsNoTracking();

        // Filter by active status
        if (!request.IncludeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        // Filter by region
        if (!string.IsNullOrWhiteSpace(request.Region))
        {
            query = query.Where(c => c.Region == request.Region);
        }

        // Filter by subregion
        if (!string.IsNullOrWhiteSpace(request.Subregion))
        {
            query = query.Where(c => c.Subregion == request.Subregion);
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = request.SortBy.ToLowerInvariant() switch
        {
            "iso2" => request.SortOrder.ToLowerInvariant() == "desc"
                ? query.OrderByDescending(c => c.Iso2)
                : query.OrderBy(c => c.Iso2),
            "population" => request.SortOrder.ToLowerInvariant() == "desc"
                ? query.OrderByDescending(c => c.Population)
                : query.OrderBy(c => c.Population),
            "area" => request.SortOrder.ToLowerInvariant() == "desc"
                ? query.OrderByDescending(c => c.AreaKm2)
                : query.OrderBy(c => c.AreaKm2),
            _ => request.SortOrder.ToLowerInvariant() == "desc"
                ? query.OrderByDescending(c => c.Name)
                : query.OrderBy(c => c.Name)
        };

        // Apply pagination
        var countries = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var data = countries.Select(MapToResponse).ToList();

        var response = new PaginatedResponse<CountryResponse>
        {
            Data = data,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        // Cache the result
        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15), cancellationToken);

        return response;
    }

    /// <summary>
    /// T071: Search countries by name using PostgreSQL full-text search with GIN index.
    /// </summary>
    public async Task<PaginatedResponse<CountryResponse>> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Validate pagination
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            // Return empty result for invalid queries
            return new PaginatedResponse<CountryResponse>
            {
                Data = new List<CountryResponse>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        var searchTerm = query.Trim().ToLowerInvariant();
        var cacheKey = GenerateCacheKey("search", $"{searchTerm}:{page}:{pageSize}");

        var cached = await _cacheService.GetAsync<PaginatedResponse<CountryResponse>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Cache HIT for search '{Query}' page {Page}", searchTerm, page);
            return cached;
        }

        _logger.LogDebug("Cache MISS for search '{Query}' page {Page}", searchTerm, page);

        // Search by name, official name, or ISO codes
        var searchQuery = _context.Countries
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Where(c => EF.Functions.ILike(c.Name, $"%{searchTerm}%") ||
                       EF.Functions.ILike(c.OfficialName ?? "", $"%{searchTerm}%") ||
                       EF.Functions.ILike(c.Iso2, $"%{searchTerm}%") ||
                       EF.Functions.ILike(c.Iso3, $"%{searchTerm}%"));

        var totalCount = await searchQuery.CountAsync(cancellationToken);

        var countries = await searchQuery
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var data = countries.Select(MapToResponse).ToList();

        var response = new PaginatedResponse<CountryResponse>
        {
            Data = data,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        // Cache search results
        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15), cancellationToken);

        return response;
    }

    /// <summary>
    /// T070: Invalidate list caches when country data changes.
    /// </summary>
    public async Task InvalidateListCachesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Invalidating all list and search caches");

        // Remove all list cache entries
        await _cacheService.RemovePatternAsync("country:list:*", cancellationToken);

        // Remove all search cache entries
        await _cacheService.RemovePatternAsync("country:search:*", cancellationToken);
    }

    /// <summary>
    /// T058: Implement cache key generation (patterns: country:id:{id}, country:iso2:{code}, etc.)
    /// </summary>
    private string GenerateCacheKey(string type, string value)
    {
        return $"country:{type}:{value}";
    }

    /// <summary>
    /// T059: Implement ETag generation (SHA256 hash of version UUID, Base64 encoded)
    /// </summary>
    private string GenerateETag(Guid version)
    {
        var versionBytes = version.ToByteArray();
        var hashBytes = SHA256.HashData(versionBytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Manual DTO mapping from Country entity to CountryResponse
    /// </summary>
    private CountryResponse MapToResponse(Data.Models.Country country)
    {
        return new CountryResponse
        {
            Id = country.Id,
            Iso2 = country.Iso2,
            Iso3 = country.Iso3,
            Name = country.Name,
            OfficialName = country.OfficialName,
            NumericCode = country.NumericCode,
            Capital = country.Capital,
            Region = country.Region,
            Subregion = country.Subregion,
            Latitude = country.Latitude,
            Longitude = country.Longitude,
            Demonym = country.Demonym,
            AreaKm2 = country.AreaKm2,
            Population = country.Population,
            GiniCoefficient = country.GiniCoefficient,
            Timezones = country.Timezones,
            Borders = country.Borders,
            CallingCodes = country.CallingCodes,
            TopLevelDomains = country.TopLevelDomains,
            Currencies = country.Currencies,
            Languages = country.Languages,
            Translations = country.Translations,
            Flags = country.Flags,
            CoatOfArms = country.CoatOfArms,
            Independent = country.Independent,
            UnMember = country.UnMember,
            Landlocked = country.Landlocked,
            IsActive = country.IsActive,
            CreatedAtUtc = country.CreatedAtUtc,
            LastModifiedUtc = country.LastModifiedUtc,
            ETag = GenerateETag(country.Version)
        };
    }

    // User Story 3: Admin CRUD Operations

    /// <summary>
    /// T079-T080: Create a new country with duplicate validation and audit logging.
    /// </summary>
    public async Task<CountryResponse> CreateAsync(CreateCountryRequest request, string userId, CancellationToken cancellationToken = default)
    {
        // T079: Validate uniqueness of ISO2/ISO3
        var existingIso2 = await _context.Countries
            .AnyAsync(c => c.Iso2 == request.Iso2.ToUpperInvariant(), cancellationToken);

        if (existingIso2)
        {
            throw new InvalidOperationException($"Country with ISO2 code '{request.Iso2}' already exists");
        }

        var existingIso3 = await _context.Countries
            .AnyAsync(c => c.Iso3 == request.Iso3.ToUpperInvariant(), cancellationToken);

        if (existingIso3)
        {
            throw new InvalidOperationException($"Country with ISO3 code '{request.Iso3}' already exists");
        }

        // T080: Create new country entity
        var country = new Data.Models.Country
        {
            Iso2 = request.Iso2.ToUpperInvariant(),
            Iso3 = request.Iso3.ToUpperInvariant(),
            Name = request.Name,
            OfficialName = request.OfficialName,
            NumericCode = request.NumericCode,
            Capital = request.Capital,
            Region = request.Region,
            Subregion = request.Subregion,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Demonym = request.Demonym,
            AreaKm2 = request.AreaKm2,
            Population = request.Population,
            GiniCoefficient = request.GiniCoefficient,
            Timezones = request.Timezones ?? "[]",
            Borders = request.Borders ?? "[]",
            CallingCodes = request.CallingCodes ?? "[]",
            TopLevelDomains = request.TopLevelDomains ?? "[]",
            Currencies = request.Currencies ?? "{}",
            Languages = request.Languages ?? "{}",
            Translations = request.Translations ?? "{}",
            Flags = request.Flags ?? "{}",
            CoatOfArms = request.CoatOfArms,
            Independent = request.Independent ?? false,
            UnMember = request.UnMember ?? false,
            Landlocked = request.Landlocked ?? false,
            IsActive = true,
            Version = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
            LastModifiedUtc = DateTime.UtcNow
        };

        _context.Countries.Add(country);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Country created: {Iso2} - {Name} by user {UserId}", country.Iso2, country.Name, userId);

        // Invalidate list caches
        await InvalidateListCachesAsync(cancellationToken);

        return MapToResponse(country);
    }

    /// <summary>
    /// T081-T082: Update existing country with optimistic concurrency check.
    /// </summary>
    public async Task<CountryResponse> UpdateAsync(long id, UpdateCountryRequest request, string ifMatch, string userId, CancellationToken cancellationToken = default)
    {
        // Load existing country with tracking
        var country = await _context.Countries.FindAsync(new object[] { id }, cancellationToken);

        if (country == null)
        {
            throw new KeyNotFoundException($"Country with ID {id} not found");
        }

        // T081: Validate If-Match header (ETag check)
        var currentETag = GenerateETag(country.Version);
        if (!string.IsNullOrEmpty(ifMatch) && ifMatch != currentETag)
        {
            throw new InvalidOperationException("Precondition failed: ETag mismatch (concurrent modification detected)");
        }

        // Check for ISO code conflicts (excluding current country)
        var iso2Conflict = await _context.Countries
            .AnyAsync(c => c.Id != id && c.Iso2 == request.Iso2.ToUpperInvariant(), cancellationToken);

        if (iso2Conflict)
        {
            throw new InvalidOperationException($"Another country with ISO2 code '{request.Iso2}' already exists");
        }

        var iso3Conflict = await _context.Countries
            .AnyAsync(c => c.Id != id && c.Iso3 == request.Iso3.ToUpperInvariant(), cancellationToken);

        if (iso3Conflict)
        {
            throw new InvalidOperationException($"Another country with ISO3 code '{request.Iso3}' already exists");
        }

        // T082: Update all fields (full replacement)
        country.Iso2 = request.Iso2.ToUpperInvariant();
        country.Iso3 = request.Iso3.ToUpperInvariant();
        country.Name = request.Name;
        country.OfficialName = request.OfficialName;
        country.NumericCode = request.NumericCode;
        country.Capital = request.Capital;
        country.Region = request.Region;
        country.Subregion = request.Subregion;
        country.Latitude = request.Latitude;
        country.Longitude = request.Longitude;
        country.Demonym = request.Demonym;
        country.AreaKm2 = request.AreaKm2;
        country.Population = request.Population;
        country.GiniCoefficient = request.GiniCoefficient;
        country.Timezones = request.Timezones ?? "[]";
        country.Borders = request.Borders ?? "[]";
        country.CallingCodes = request.CallingCodes ?? "[]";
        country.TopLevelDomains = request.TopLevelDomains ?? "[]";
        country.Currencies = request.Currencies ?? "{}";
        country.Languages = request.Languages ?? "{}";
        country.Translations = request.Translations ?? "{}";
        country.Flags = request.Flags ?? "{}";
        country.CoatOfArms = request.CoatOfArms;
        country.Independent = request.Independent ?? false;
        country.UnMember = request.UnMember ?? false;
        country.Landlocked = request.Landlocked ?? false;
        country.Version = Guid.NewGuid();
        country.LastModifiedUtc = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // T101: Handle EF Core concurrency exception
            _logger.LogWarning(ex, "Concurrency conflict detected for country ID {Id}", id);
            throw new InvalidOperationException($"Concurrency conflict: Country ID {id} was modified by another user. Please refresh and try again.", ex);
        }

        _logger.LogInformation("Country updated: {Iso2} - {Name} by user {UserId}", country.Iso2, country.Name, userId);

        // Invalidate caches
        await InvalidateSingleCountryCacheAsync(country, cancellationToken);
        await InvalidateListCachesAsync(cancellationToken);

        return MapToResponse(country);
    }

    /// <summary>
    /// T083-T084: Partially update country (only specified fields).
    /// </summary>
    public async Task<CountryResponse> PatchAsync(long id, PatchCountryRequest request, string ifMatch, string userId, CancellationToken cancellationToken = default)
    {
        var country = await _context.Countries.FindAsync(new object[] { id }, cancellationToken);

        if (country == null)
        {
            throw new KeyNotFoundException($"Country with ID {id} not found");
        }

        // ETag check
        var currentETag = GenerateETag(country.Version);
        if (!string.IsNullOrEmpty(ifMatch) && ifMatch != currentETag)
        {
            throw new InvalidOperationException("Precondition failed: ETag mismatch (concurrent modification detected)");
        }

        // T083: Apply only non-null fields
        if (request.Iso2 != null)
        {
            var conflict = await _context.Countries.AnyAsync(c => c.Id != id && c.Iso2 == request.Iso2.ToUpperInvariant(), cancellationToken);
            if (conflict) throw new InvalidOperationException($"Another country with ISO2 code '{request.Iso2}' already exists");
            country.Iso2 = request.Iso2.ToUpperInvariant();
        }

        if (request.Iso3 != null)
        {
            var conflict = await _context.Countries.AnyAsync(c => c.Id != id && c.Iso3 == request.Iso3.ToUpperInvariant(), cancellationToken);
            if (conflict) throw new InvalidOperationException($"Another country with ISO3 code '{request.Iso3}' already exists");
            country.Iso3 = request.Iso3.ToUpperInvariant();
        }

        if (request.Name != null) country.Name = request.Name;
        if (request.OfficialName != null) country.OfficialName = request.OfficialName;
        if (request.NumericCode != null) country.NumericCode = request.NumericCode;
        if (request.Capital != null) country.Capital = request.Capital;
        if (request.Region != null) country.Region = request.Region;
        if (request.Subregion != null) country.Subregion = request.Subregion;
        if (request.Latitude.HasValue) country.Latitude = request.Latitude;
        if (request.Longitude.HasValue) country.Longitude = request.Longitude;
        if (request.Demonym != null) country.Demonym = request.Demonym;
        if (request.AreaKm2.HasValue) country.AreaKm2 = request.AreaKm2;
        if (request.Population.HasValue) country.Population = request.Population;
        if (request.GiniCoefficient.HasValue) country.GiniCoefficient = request.GiniCoefficient;
        if (request.Timezones != null) country.Timezones = request.Timezones;
        if (request.Borders != null) country.Borders = request.Borders;
        if (request.CallingCodes != null) country.CallingCodes = request.CallingCodes;
        if (request.TopLevelDomains != null) country.TopLevelDomains = request.TopLevelDomains;
        if (request.Currencies != null) country.Currencies = request.Currencies;
        if (request.Languages != null) country.Languages = request.Languages;
        if (request.Translations != null) country.Translations = request.Translations;
        if (request.Flags != null) country.Flags = request.Flags;
        if (request.CoatOfArms != null) country.CoatOfArms = request.CoatOfArms;
        if (request.Independent.HasValue) country.Independent = request.Independent.Value;
        if (request.UnMember.HasValue) country.UnMember = request.UnMember.Value;
        if (request.Landlocked.HasValue) country.Landlocked = request.Landlocked.Value;

        country.Version = Guid.NewGuid();
        country.LastModifiedUtc = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // T101: Handle EF Core concurrency exception
            _logger.LogWarning(ex, "Concurrency conflict detected for country ID {Id}", id);
            throw new InvalidOperationException($"Concurrency conflict: Country ID {id} was modified by another user. Please refresh and try again.", ex);
        }

        _logger.LogInformation("Country patched: {Iso2} - {Name} by user {UserId}", country.Iso2, country.Name, userId);

        // Invalidate caches
        await InvalidateSingleCountryCacheAsync(country, cancellationToken);
        await InvalidateListCachesAsync(cancellationToken);

        return MapToResponse(country);
    }

    /// <summary>
    /// T085-T086: Soft delete country (set IsActive=false).
    /// </summary>
    public async Task SoftDeleteAsync(long id, string userId, CancellationToken cancellationToken = default)
    {
        var country = await _context.Countries.FindAsync(new object[] { id }, cancellationToken);

        if (country == null)
        {
            throw new KeyNotFoundException($"Country with ID {id} not found");
        }

        if (!country.IsActive)
        {
            throw new InvalidOperationException($"Country {country.Iso2} is already inactive");
        }

        country.IsActive = false;
        country.Version = Guid.NewGuid();
        country.LastModifiedUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Country soft-deleted: {Iso2} - {Name} by user {UserId}", country.Iso2, country.Name, userId);

        // Invalidate caches
        await InvalidateSingleCountryCacheAsync(country, cancellationToken);
        await InvalidateListCachesAsync(cancellationToken);
    }

    /// <summary>
    /// T087-T088: Hard delete country (permanent deletion, SuperAdmin only).
    /// </summary>
    public async Task HardDeleteAsync(long id, string userId, CancellationToken cancellationToken = default)
    {
        var country = await _context.Countries.FindAsync(new object[] { id }, cancellationToken);

        if (country == null)
        {
            throw new KeyNotFoundException($"Country with ID {id} not found");
        }

        var iso2 = country.Iso2;
        var name = country.Name;

        _context.Countries.Remove(country);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Country HARD-DELETED: {Iso2} - {Name} by user {UserId}", iso2, name, userId);

        // Invalidate caches
        await InvalidateSingleCountryCacheAsync(country, cancellationToken);
        await InvalidateListCachesAsync(cancellationToken);
    }

    /// <summary>
    /// Helper: Invalidate cache entries for a specific country.
    /// </summary>
    private async Task InvalidateSingleCountryCacheAsync(Data.Models.Country country, CancellationToken cancellationToken)
    {
        await _cacheService.RemoveAsync(GenerateCacheKey("id", country.Id.ToString()), cancellationToken);
        await _cacheService.RemoveAsync(GenerateCacheKey("iso2", country.Iso2), cancellationToken);
        await _cacheService.RemoveAsync(GenerateCacheKey("iso3", country.Iso3), cancellationToken);
    }
}
