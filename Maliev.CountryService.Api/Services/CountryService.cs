using Maliev.CountryService.Api.Models.Common;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Maliev.CountryService.Api.Models; // Added
using Maliev.CountryService.Data.Entities; // Added

namespace Maliev.CountryService.Api.Services;

/// <summary>
/// Service implementation for country data operations with caching support.
/// T122: Graceful degradation with cache-only fallback when database is unavailable.
/// </summary>
public class CountryService : ICountryService
{
    private readonly CountryDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CountryService> _logger;
    private readonly DegradationContext _degradationContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountryService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="cacheService">The cache service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="degradationContext">The degradation context.</param>
    public CountryService(
        CountryDbContext context,
        ICacheService cacheService,
        ILogger<CountryService> logger,
        DegradationContext degradationContext)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
        _degradationContext = degradationContext;
    }

    /// <summary>
    /// Retrieves a country by its unique identifier with cache-first strategy and graceful degradation support.
    /// </summary>
    /// <param name="id">The country ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The country response, or null if not found.</returns>
    public async Task<CountryResponse?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey("id", id.ToString());

        // Try cache first
        var cached = await _cacheService.GetAsync<CountryResponse>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Cache HIT for country ID {Id}", id);
            cached.XServedFromCache = true; // Set header property
            if (_degradationContext.IsDegraded) // If in degraded mode, it's stale data
            {
                cached.XCacheStale = true;
            }
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
                staleData.XServedFromCache = true; // Set header property
                staleData.XCacheStale = true; // Set header property
                _logger.LogInformation("Serving cached data in degraded mode for country ID {Id}", id);
                return staleData;
            }

            // No cache available - rethrow
            _logger.LogError("No cache available for country ID {Id}, cannot serve request in degraded mode", id);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a country by its ISO2 code with cache-first strategy and graceful degradation support.
    /// </summary>
    /// <param name="iso2">The ISO2 country code.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The country response, or null if not found.</returns>
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
                staleData.XServedFromCache = true; // Set header property
                staleData.XCacheStale = true; // Set header property
                _logger.LogInformation("Serving cached data in degraded mode for ISO2 {Iso2}", iso2);
                return staleData;
            }

            _logger.LogError("No cache available for ISO2 {Iso2}, cannot serve request in degraded mode", iso2);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a country by its ISO3 code with cache-first strategy and graceful degradation support.
    /// </summary>
    /// <param name="iso3">The ISO3 country code.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The country response, or null if not found.</returns>
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
                staleData.XServedFromCache = true; // Set header property
                staleData.XCacheStale = true; // Set header property
                _logger.LogInformation("Serving cached data in degraded mode for ISO3 {Iso3}", iso3);
                return staleData;
            }

            _logger.LogError("No cache available for ISO3 {Iso3}, cannot serve request in degraded mode", iso3);
            throw;
        }
    }

    /// <summary>
    /// T069: Lists countries with pagination, filtering, and sorting.
    /// T070: Cache list results with cache key based on all parameters.
    /// </summary>
    /// <param name="request">The list request containing filter and pagination parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A paginated response containing country data.</returns>
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
        IQueryable<Data.Entities.Country> query = _context.Countries.AsNoTracking();

        // Filter by active status - need to ignore global query filter if including inactive
        if (request.IncludeInactive)
        {
            query = query.IgnoreQueryFilters();
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

        // Default sorting - commented out as switch handles default
        // var orderedQuery = query.OrderBy(c => c.Name ?? string.Empty);

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
    /// T071: Searches countries by name using PostgreSQL full-text search with GIN index.
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="page">The page number for pagination.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A paginated response containing matching country data.</returns>
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
                       EF.Functions.ILike(c.Iso3!, $"%{searchTerm}%")); // Iso3 is nullable, so use !

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
    /// T070: Invalidates list caches when country data changes.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvalidateListCachesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Invalidating all list and search caches");

        // Remove all list cache entries
        await _cacheService.RemovePatternAsync("country:list:*", cancellationToken);

        // Remove all search cache entries
        await _cacheService.RemovePatternAsync("country:search:*", cancellationToken);
    }

    /// <summary>
    /// T058: Generates cache keys following standard patterns (country:id:{id}, country:iso2:{code}, etc.).
    /// </summary>
    /// <param name="type">The type of cache key (e.g., "id", "iso2", "iso3").</param>
    /// <param name="value">The value to include in the cache key.</param>
    /// <returns>A formatted cache key string.</returns>
    private string GenerateCacheKey(string type, string value)
    {
        return $"country:{type}:{value}";
    }

    /// <summary>
    /// Logs an audit entry for country modifications.
    /// </summary>
    /// <param name="countryId">The ID of the country being audited.</param>
    /// <param name="action">The action performed (e.g., CREATE, UPDATE, SOFT_DELETE, HARD_DELETE).</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <param name="changesJson">Optional JSON string representing the changes made.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LogAuditAsync(long? countryId, string action, string userId, string? changesJson, CancellationToken cancellationToken)
    {
        var auditLog = new AuditLog
        {
            CountryId = countryId,
            Action = action,
            UserId = userId,
            TimestampUtc = DateTime.UtcNow,
            Changes = changesJson,
        };
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// T059: Generates ETags for optimistic concurrency control (SHA256 hash of version UUID, Base64 encoded).
    /// </summary>
    /// <param name="version">The version GUID of the entity.</param>
    /// <returns>A Base64-encoded SHA256 hash string for use as an ETag.</returns>
    private string GenerateETag(Guid version)
    {
        var versionBytes = version.ToByteArray();
        var hashBytes = SHA256.HashData(versionBytes);
        return $"\"{Convert.ToBase64String(hashBytes)}\"";
    }

    /// <summary>
    /// Maps a Country entity to a CountryResponse DTO.
    /// </summary>
    /// <param name="country">The country entity to map.</param>
    /// <returns>A country response DTO.</returns>
#nullable disable
    private CountryResponse MapToResponse(Data.Entities.Country country)
    {
        return new CountryResponse
        {
            Id = country.Id,
            Iso2 = country.Iso2,
            Iso3 = country.Iso3 ?? string.Empty,
            Name = country.Name,
            OfficialName = country.OfficialName,
            NumericCode = country.NumericCode,
            Capital = country.Capital,
            Region = country.Region,
            Subregion = country.Subregion,
            Latitude = (double?)country.Latitude,
            Longitude = (double?)country.Longitude,
            Demonym = country.Demonym,
            AreaKm2 = (double?)country.AreaKm2,
            Population = country.Population,
            GiniCoefficient = (double?)country.GiniCoefficient,
            Timezones = DeserializeJson(country.Timezones),
            Borders = DeserializeJson(country.Borders),
            CallingCodes = DeserializeJson(country.CallingCodes),
            TopLevelDomains = DeserializeJson(country.TopLevelDomains),
            Currencies = DeserializeJson(country.Currencies),
            Languages = DeserializeJson(country.Languages),
            Translations = DeserializeJson(country.Translations),
            Flags = DeserializeJson(country.Flags),
            CoatOfArms = DeserializeJson(country.CoatOfArms),
            Independent = country.Independent,
            UnMember = country.UnMember,
            Landlocked = country.Landlocked,
            IsActive = country.IsActive,
            CreatedAtUtc = country.CreatedAtUtc,
            LastModifiedUtc = country.LastModifiedUtc,
            ETag = GenerateETag(country.Version)
        };
    }
#nullable restore

    private JsonElement DeserializeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return JsonDocument.Parse("{}").RootElement;
        }

        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);

            if (element.ValueKind == JsonValueKind.String)
            {
                var innerJson = element.GetString();
                if (!string.IsNullOrWhiteSpace(innerJson) &&
                    (innerJson.TrimStart().StartsWith("[") || innerJson.TrimStart().StartsWith("{")))
                {
                    try
                    {
                        return JsonSerializer.Deserialize<JsonElement>(innerJson);
                    }
                    catch
                    {
                        return element;
                    }
                }
            }
            return element;
        }
        catch
        {
            return JsonDocument.Parse("{}").RootElement;
        }
    }

    // User Story 3: Admin CRUD Operations

    /// <summary>
    /// T079-T080: Creates a new country with duplicate validation and audit logging.
    /// </summary>
    /// <param name="request">The country creation request.</param>
    /// <param name="userId">The ID of the user creating the country.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created country response.</returns>
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
            .AnyAsync(c => c.Iso3 == (request.Iso3 ?? "").ToUpperInvariant(), cancellationToken);

        if (existingIso3)
        {
            throw new InvalidOperationException($"Country with ISO3 code '{request.Iso3}' already exists");
        }

        // T080: Create new country entity
#nullable disable
        var country = new Data.Entities.Country
        {
            Iso2 = request.Iso2.ToUpperInvariant(),
            Iso3 = request.Iso3.ToUpperInvariant(),
            Name = request.Name,
            OfficialName = request.OfficialName,
            NumericCode = request.NumericCode,
            Capital = request.Capital,
            Region = request.Region,
            Subregion = request.Subregion,
            Latitude = (double?)request.Latitude,
            Longitude = (double?)request.Longitude,
            Demonym = request.Demonym,
            AreaKm2 = (double?)request.AreaKm2,
            Population = request.Population,
            GiniCoefficient = (double?)request.GiniCoefficient,
            Timezones = request.Timezones ?? "{}",
            Borders = request.Borders ?? "{}",
            CallingCodes = request.CallingCodes ?? "{}",
            TopLevelDomains = request.TopLevelDomains ?? "{}",
            Currencies = request.Currencies ?? "{}",
            Languages = request.Languages ?? "{}",
            Translations = request.Translations ?? "{}",
            Flags = request.Flags ?? "{}",
            CoatOfArms = request.CoatOfArms ?? "{}",
            Independent = request.Independent ?? false,
            UnMember = request.UnMember ?? false,
            Landlocked = request.Landlocked ?? false,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            LastModifiedUtc = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedBy = userId,
            Version = Guid.NewGuid()
        };
#nullable restore

        _context.Countries.Add(country);
        await _context.SaveChangesAsync(cancellationToken);

        // Log audit
        await LogAuditAsync(country.Id, "CREATE", userId, JsonSerializer.Serialize(country), cancellationToken);

        _logger.LogInformation("Country created: {Iso2} - {Name} by user {UserId}", country.Iso2, country.Name, userId);

        // Invalidate list caches
        await InvalidateListCachesAsync(cancellationToken);

        return MapToResponse(country);
    }

    /// <summary>
    /// T081-T082: Updates an existing country (full replacement) with optimistic concurrency check.
    /// </summary>
    /// <param name="id">The ID of the country to update.</param>
    /// <param name="request">The country update request.</param>
    /// <param name="ifMatch">The ETag value for optimistic concurrency control.</param>
    /// <param name="userId">The ID of the user updating the country.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated country response.</returns>
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
            .AnyAsync(c => c.Id != id && c.Iso3 == (request.Iso3 ?? "").ToUpperInvariant(), cancellationToken);

        if (iso3Conflict)
        {
            throw new InvalidOperationException($"Another country with ISO3 code '{request.Iso3}' already exists");
        }

        // T082: Update all fields (full replacement)
#nullable disable
        country.Iso2 = request.Iso2.ToUpperInvariant();
        country.Iso3 = request.Iso3.ToUpperInvariant();
        country.Name = request.Name;
        country.OfficialName = request.OfficialName;
        country.NumericCode = request.NumericCode;
        country.Capital = request.Capital;
        country.Region = request.Region;
        country.Subregion = request.Subregion;
        country.Latitude = (double?)request.Latitude;
        country.Longitude = (double?)request.Longitude;
        country.Demonym = request.Demonym;
        country.AreaKm2 = (double?)request.AreaKm2;
        country.Population = request.Population;
        country.GiniCoefficient = (double?)request.GiniCoefficient;
        country.Timezones = request.Timezones ?? "{}";
        country.Borders = request.Borders ?? "{}";
        country.CallingCodes = request.CallingCodes ?? "{}";
        country.TopLevelDomains = request.TopLevelDomains ?? "{}";
        country.Currencies = request.Currencies ?? "{}";
        country.Languages = request.Languages ?? "{}";
        country.Translations = request.Translations ?? "{}";
        country.Flags = request.Flags ?? "{}";
        country.CoatOfArms = request.CoatOfArms ?? "{}";
        country.Independent = request.Independent ?? false;
        country.UnMember = request.UnMember ?? false;
        country.Landlocked = request.Landlocked ?? false;
        country.LastModifiedUtc = DateTime.UtcNow;
        country.UpdatedBy = userId;
        country.Version = Guid.NewGuid();
#nullable restore

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

        // Log audit
        await LogAuditAsync(country.Id, "UPDATE", userId, JsonSerializer.Serialize(request), cancellationToken);

        _logger.LogInformation("Country updated: {Iso2} - {Name} by user {UserId}", country.Iso2, country.Name, userId);

        // Invalidate caches
        await InvalidateSingleCountryCacheAsync(country, cancellationToken);
        await InvalidateListCachesAsync(cancellationToken);

        return MapToResponse(country);
    }

    /// <summary>
    /// T083-T084: Partially updates an existing country (only specified fields).
    /// </summary>
    /// <param name="id">The ID of the country to patch.</param>
    /// <param name="request">The country patch request.</param>
    /// <param name="ifMatch">The ETag value for optimistic concurrency control.</param>
    /// <param name="userId">The ID of the user patching the country.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The patched country response.</returns>
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
#nullable disable
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
        if (request.Latitude.HasValue) country.Latitude = (double?)request.Latitude;
        if (request.Longitude.HasValue) country.Longitude = (double?)request.Longitude;
        if (request.Demonym != null) country.Demonym = request.Demonym;
        if (request.AreaKm2.HasValue) country.AreaKm2 = (double?)request.AreaKm2;
        if (request.Population.HasValue) country.Population = request.Population;
        if (request.GiniCoefficient.HasValue) country.GiniCoefficient = (double?)request.GiniCoefficient;
        if (request.Timezones is not null) country.Timezones = request.Timezones;
        if (request.Borders is not null) country.Borders = request.Borders;
        if (request.CallingCodes is not null) country.CallingCodes = request.CallingCodes;
        if (request.TopLevelDomains is not null) country.TopLevelDomains = request.TopLevelDomains;
        if (request.Currencies is not null) country.Currencies = request.Currencies;
        if (request.Languages is not null) country.Languages = request.Languages;
        if (request.Translations is not null) country.Translations = request.Translations;
        if (request.Flags is not null) country.Flags = request.Flags;
        if (request.CoatOfArms is not null) country.CoatOfArms = request.CoatOfArms;
        if (request.Independent.HasValue) country.Independent = request.Independent.Value;
        if (request.UnMember.HasValue) country.UnMember = request.UnMember.Value;
        if (request.Landlocked.HasValue) country.Landlocked = request.Landlocked.Value;
        country.LastModifiedUtc = DateTime.UtcNow;
        country.UpdatedBy = userId;
        country.Version = Guid.NewGuid();
#nullable restore

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

        // Log audit
        await LogAuditAsync(country.Id, "PATCH", userId, JsonSerializer.Serialize(request), cancellationToken);

        _logger.LogInformation("Country patched: {Iso2} - {Name} by user {UserId}", country.Iso2, country.Name, userId);

        // Invalidate caches
        await InvalidateSingleCountryCacheAsync(country, cancellationToken);
        await InvalidateListCachesAsync(cancellationToken);

        return MapToResponse(country);
    }

    /// <summary>
    /// T085-T086: Soft deletes a country (sets IsActive=false).
    /// </summary>
    /// <param name="id">The ID of the country to soft delete.</param>
    /// <param name="userId">The ID of the user deleting the country.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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
        country.LastModifiedUtc = DateTime.UtcNow;
        country.DeletedAt = DateTime.UtcNow;
        country.UpdatedBy = userId;
        country.Version = Guid.NewGuid();

        await _context.SaveChangesAsync(cancellationToken);

        // Log audit
        await LogAuditAsync(id, "SOFT_DELETE", userId, JsonSerializer.Serialize(new { IsActive = false, DeletedAt = country.DeletedAt }), cancellationToken);

        _logger.LogInformation("Country soft-deleted: {Iso2} - {Name} by user {UserId}", country.Iso2, country.Name, userId);

        // Invalidate caches
        await InvalidateSingleCountryCacheAsync(country, cancellationToken);
        await InvalidateListCachesAsync(cancellationToken);
    }

    /// <summary>
    /// T087-T088: Permanently deletes a country (SuperAdmin only).
    /// </summary>
    /// <param name="id">The ID of the country to permanently delete.</param>
    /// <param name="userId">The ID of the user deleting the country.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HardDeleteAsync(long id, string userId, CancellationToken cancellationToken = default)
    {
        var country = await _context.Countries.FindAsync(new object[] { id }, cancellationToken);

        if (country == null)
        {
            throw new KeyNotFoundException($"Country with ID {id} not found");
        }

        var iso2 = country.Iso2;
        var name = country.Name;

        // Log audit BEFORE deleting (FK constraint requires country to exist when logging)
        await LogAuditAsync(id, "HARD_DELETE", userId, JsonSerializer.Serialize(new { Id = id, Iso2 = iso2, Name = name }), cancellationToken);

        _context.Countries.Remove(country);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Country HARD-DELETED: {Iso2} - {Name} by user {UserId}", iso2, name, userId);

        // Invalidate caches
        await InvalidateSingleCountryCacheAsync(country, cancellationToken);
        await InvalidateListCachesAsync(cancellationToken);
    }

    /// <summary>
    /// Restores a soft-deleted country (sets IsActive=true).
    /// </summary>
    /// <param name="id">The ID of the country to restore.</param>
    /// <param name="userId">The ID of the user restoring the country.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RestoreAsync(long id, string userId, CancellationToken cancellationToken = default)
    {
        // Need to use IgnoreQueryFilters to find inactive country
        var country = await _context.Countries
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (country == null)
        {
            throw new KeyNotFoundException($"Country with ID {id} not found");
        }

        if (country.IsActive)
        {
            throw new InvalidOperationException($"Country {country.Iso2} is already active");
        }

        country.IsActive = true;
        country.LastModifiedUtc = DateTime.UtcNow;
        country.DeletedAt = null;
        country.UpdatedBy = userId;
        country.Version = Guid.NewGuid();

        await _context.SaveChangesAsync(cancellationToken);

        // Log audit
        await LogAuditAsync(id, "RESTORE", userId, JsonSerializer.Serialize(new { IsActive = true, RestoredAt = DateTime.UtcNow }), cancellationToken);

        _logger.LogInformation("Country restored: {Iso2} - {Name} by user {UserId}", country.Iso2, country.Name, userId);

        // Invalidate caches
        await InvalidateSingleCountryCacheAsync(country, cancellationToken);
        await InvalidateListCachesAsync(cancellationToken);
    }

    /// <summary>
    /// Invalidates all cache entries for a specific country (by ID, ISO2, and ISO3).
    /// </summary>
    /// <param name="country">The country entity whose cache entries should be invalidated.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task InvalidateSingleCountryCacheAsync(Data.Entities.Country country, CancellationToken cancellationToken)
    {
        await _cacheService.RemoveAsync(GenerateCacheKey("id", country.Id.ToString()), cancellationToken);
        await _cacheService.RemoveAsync(GenerateCacheKey("iso2", country.Iso2), cancellationToken);
        if (!string.IsNullOrEmpty(country.Iso3))
        {
            await _cacheService.RemoveAsync(GenerateCacheKey("iso3", country.Iso3), cancellationToken);
        }
    }
}
