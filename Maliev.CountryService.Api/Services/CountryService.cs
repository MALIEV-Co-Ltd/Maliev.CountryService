using Maliev.CountryService.Api.Models.Common;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Data;
using Maliev.CountryService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;

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
    /// Retrieves a country by its unique identifier with cache-first strategy.
    /// </summary>
    public async Task<CountryResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey("id", id.ToString());

        var cached = await _cacheService.GetAsync<CountryResponse>(cacheKey, cancellationToken);
        if (cached != null)
        {
            cached.XServedFromCache = true;
            if (_degradationContext.IsDegraded) cached.XCacheStale = true;
            return cached;
        }

        try
        {
            var country = await _context.Countries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, cancellationToken);

            if (country == null) return null;

            var response = MapToResponse(country);
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15), cancellationToken);
            return response;
        }
        catch (Exception ex) when (ex is Npgsql.NpgsqlException or Microsoft.EntityFrameworkCore.DbUpdateException or System.Net.Sockets.SocketException)
        {
            _degradationContext.IsDegraded = true;
            _degradationContext.DegradationReason = "Database unavailable";
            throw;
        }
    }

    /// <summary>
    /// Retrieves a country by its ISO2 code.
    /// </summary>
    public async Task<CountryResponse?> GetByIso2Async(string iso2, CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey("iso2", iso2.ToUpperInvariant());
        var cached = await _cacheService.GetAsync<CountryResponse>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        try
        {
            var country = await _context.Countries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Iso2 == iso2.ToUpperInvariant() && c.IsActive, cancellationToken);

            if (country == null) return null;

            var response = MapToResponse(country);
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15), cancellationToken);
            return response;
        }
        catch (Exception)
        {
            var staleData = await _cacheService.GetAsync<CountryResponse>(cacheKey, cancellationToken);
            if (staleData != null)
            {
                _degradationContext.IsDegraded = true;
                staleData.XServedFromCache = true;
                staleData.XCacheStale = true;
                return staleData;
            }
            throw;
        }
    }

    /// <summary>
    /// Retrieves a country by its ISO3 code.
    /// </summary>
    public async Task<CountryResponse?> GetByIso3Async(string iso3, CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey("iso3", iso3.ToUpperInvariant());
        var cached = await _cacheService.GetAsync<CountryResponse>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        try
        {
            var country = await _context.Countries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Iso3 == iso3.ToUpperInvariant() && c.IsActive, cancellationToken);

            if (country == null) return null;

            var response = MapToResponse(country);
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15), cancellationToken);
            return response;
        }
        catch (Exception)
        {
            var staleData = await _cacheService.GetAsync<CountryResponse>(cacheKey, cancellationToken);
            if (staleData != null)
            {
                _degradationContext.IsDegraded = true;
                staleData.XServedFromCache = true;
                staleData.XCacheStale = true;
                return staleData;
            }
            throw;
        }
    }

    /// <summary>
    /// Lists countries with pagination, filtering, and sorting.
    /// Returns all countries if no pagination parameters are provided.
    /// </summary>
    public async Task<PaginatedResponse<CountryResponse>> ListAsync(CountryListRequest request, CancellationToken cancellationToken = default)
    {
        bool returnAll = !request.Page.HasValue && !request.PageSize.HasValue;
        var page = Math.Max(1, request.Page ?? 1);
        var pageSize = Math.Clamp(request.PageSize ?? 1000, 1, 1000);

        var filterKey = $"{request.Region ?? "all"}:{request.Subregion ?? "all"}:{request.SortBy}:{request.SortOrder}:{request.IncludeInactive}";
        var paginationKey = returnAll ? "all" : $"{page}:{pageSize}";
        var cacheKey = GenerateCacheKey("list", $"{paginationKey}:{filterKey}");

        var cached = await _cacheService.GetAsync<PaginatedResponse<CountryResponse>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        IQueryable<Data.Entities.Country> query = _context.Countries.AsNoTracking();
        if (request.IncludeInactive) query = query.IgnoreQueryFilters();
        if (!string.IsNullOrWhiteSpace(request.Region)) query = query.Where(c => c.Region == request.Region);
        if (!string.IsNullOrWhiteSpace(request.Subregion)) query = query.Where(c => c.Subregion == request.Subregion);

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy.ToLowerInvariant() switch
        {
            "iso2" => request.SortOrder.ToLowerInvariant() == "desc" ? query.OrderByDescending(c => c.Iso2) : query.OrderBy(c => c.Iso2),
            "population" => request.SortOrder.ToLowerInvariant() == "desc" ? query.OrderByDescending(c => c.Population) : query.OrderBy(c => c.Population),
            "area" => request.SortOrder.ToLowerInvariant() == "desc" ? query.OrderByDescending(c => c.AreaKm2) : query.OrderBy(c => c.AreaKm2),
            _ => request.SortOrder.ToLowerInvariant() == "desc" ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name)
        };

        List<Data.Entities.Country> countries = returnAll
            ? await query.ToListAsync(cancellationToken)
            : await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var response = new PaginatedResponse<CountryResponse>
        {
            Data = countries.Select(MapToResponse).ToList(),
            Page = returnAll ? 1 : page,
            PageSize = returnAll ? totalCount : pageSize,
            TotalCount = totalCount
        };

        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromHours(1), cancellationToken);
        return response;
    }

    /// <summary>
    /// Searches countries by name using PostgreSQL full-text search.
    /// </summary>
    public async Task<PaginatedResponse<CountryResponse>> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return new PaginatedResponse<CountryResponse> { Page = page, PageSize = pageSize };
        }

        var searchTerm = query.Trim().ToLowerInvariant();
        var cacheKey = GenerateCacheKey("search", $"{searchTerm}:{page}:{pageSize}");
        var cached = await _cacheService.GetAsync<PaginatedResponse<CountryResponse>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var searchQuery = _context.Countries.AsNoTracking().Where(c => c.IsActive)
            .Where(c => EF.Functions.ILike(c.Name, $"%{searchTerm}%") ||
                       EF.Functions.ILike(c.OfficialName ?? "", $"%{searchTerm}%") ||
                       EF.Functions.ILike(c.Iso2, $"%{searchTerm}%") ||
                       EF.Functions.ILike(c.Iso3!, $"%{searchTerm}%"));

        var totalCount = await searchQuery.CountAsync(cancellationToken);
        var countries = await searchQuery.OrderBy(c => c.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var response = new PaginatedResponse<CountryResponse>
        {
            Data = countries.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15), cancellationToken);
        return response;
    }

    /// <summary>
    /// Invalidates all list and search caches.
    /// </summary>
    public async Task InvalidateListCachesAsync(CancellationToken cancellationToken = default)
    {
        await _cacheService.RemovePatternAsync("country:list:*", cancellationToken);
        await _cacheService.RemovePatternAsync("country:search:*", cancellationToken);
    }

    private string GenerateCacheKey(string type, string value) => $"country:{type}:{value}";

    private string GenerateETag(Guid version)
    {
        var versionBytes = version.ToByteArray();
        var hashBytes = SHA256.HashData(versionBytes);
        return $"\"{Convert.ToBase64String(hashBytes)}\"";
    }

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

    private JsonElement DeserializeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return JsonDocument.Parse("{}").RootElement;
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            if (element.ValueKind == JsonValueKind.String)
            {
                var innerJson = element.GetString();
                if (!string.IsNullOrWhiteSpace(innerJson) && (innerJson.TrimStart().StartsWith("[") || innerJson.TrimStart().StartsWith("{")))
                {
                    try { return JsonSerializer.Deserialize<JsonElement>(innerJson); } catch { return element; }
                }
            }
            return element;
        }
        catch { return JsonDocument.Parse("{}").RootElement; }
    }

    /// <summary>
    /// Creates a new country.
    /// </summary>
    public async Task<CountryResponse> CreateAsync(CreateCountryRequest request, string userId, CancellationToken cancellationToken = default)
    {
        if (await _context.Countries.AnyAsync(c => c.Iso2 == request.Iso2.ToUpperInvariant(), cancellationToken))
            throw new InvalidOperationException($"Country with ISO2 code '{request.Iso2}' already exists");
        if (await _context.Countries.AnyAsync(c => c.Iso3 == (request.Iso3 ?? "").ToUpperInvariant(), cancellationToken))
            throw new InvalidOperationException($"Country with ISO3 code '{request.Iso3}' already exists");

        var country = new Data.Entities.Country
        {
            Iso2 = request.Iso2.ToUpperInvariant(),
            Iso3 = request.Iso3?.ToUpperInvariant() ?? string.Empty,
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
            Timezones = request.Timezones ?? "[]",
            Borders = request.Borders ?? "[]",
            CallingCodes = request.CallingCodes ?? "[]",
            TopLevelDomains = request.TopLevelDomains ?? "[]",
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

        _context.Countries.Add(country);
        _context.AuditLogs.Add(new AuditLog
        {
            Country = country,
            Action = "CREATE",
            UserId = userId,
            TimestampUtc = DateTime.UtcNow,
            Changes = JsonSerializer.Serialize(country)
        });

        await _context.SaveChangesAsync(cancellationToken);
        await InvalidateListCachesAsync(cancellationToken);
        return MapToResponse(country);
    }

    /// <summary>
    /// Updates an existing country.
    /// </summary>
    public async Task<CountryResponse> UpdateAsync(Guid id, UpdateCountryRequest request, string ifMatch, string userId, CancellationToken cancellationToken = default)
    {
        var country = await _context.Countries.FindAsync(new object[] { id }, cancellationToken);
        if (country == null) throw new KeyNotFoundException($"Country with ID {id} not found");

        if (!string.IsNullOrEmpty(ifMatch) && ifMatch != GenerateETag(country.Version))
            throw new InvalidOperationException("Precondition failed: ETag mismatch");

        if (await _context.Countries.AnyAsync(c => c.Id != id && c.Iso2 == request.Iso2.ToUpperInvariant(), cancellationToken))
            throw new InvalidOperationException($"Another country with ISO2 code '{request.Iso2}' already exists");
        if (await _context.Countries.AnyAsync(c => c.Id != id && c.Iso3 == (request.Iso3 ?? "").ToUpperInvariant(), cancellationToken))
            throw new InvalidOperationException($"Another country with ISO3 code '{request.Iso3}' already exists");

        country.Iso2 = request.Iso2.ToUpperInvariant();
        country.Iso3 = request.Iso3?.ToUpperInvariant() ?? string.Empty;
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
        country.Timezones = request.Timezones ?? "[]";
        country.Borders = request.Borders ?? "[]";
        country.CallingCodes = request.CallingCodes ?? "[]";
        country.TopLevelDomains = request.TopLevelDomains ?? "[]";
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

        _context.AuditLogs.Add(new AuditLog { CountryId = country.Id, Action = "UPDATE", UserId = userId, TimestampUtc = DateTime.UtcNow, Changes = JsonSerializer.Serialize(request) });
        await _context.SaveChangesAsync(cancellationToken);
        await InvalidateSingleCountryCacheAsync(country!, cancellationToken);
        await InvalidateListCachesAsync(cancellationToken);
        return MapToResponse(country);
    }

    /// <summary>
    /// Partially updates an existing country.
    /// </summary>
    public async Task<CountryResponse> PatchAsync(Guid id, PatchCountryRequest request, string ifMatch, string userId, CancellationToken cancellationToken = default)
    {
        var country = await _context.Countries.FindAsync(new object[] { id }, cancellationToken);
        if (country == null) throw new KeyNotFoundException($"Country with ID {id} not found");
        if (!string.IsNullOrEmpty(ifMatch) && ifMatch != GenerateETag(country.Version)) throw new InvalidOperationException("Precondition failed: ETag mismatch");

        if (request.Iso2 != null)
        {
            if (await _context.Countries.AnyAsync(c => c.Id != id && c.Iso2 == request.Iso2.ToUpperInvariant(), cancellationToken))
                throw new InvalidOperationException($"Another country with ISO2 code '{request.Iso2}' already exists");
            country.Iso2 = request.Iso2.ToUpperInvariant();
        }
        if (request.Iso3 != null)
        {
            if (await _context.Countries.AnyAsync(c => c.Id != id && c.Iso3 == request.Iso3.ToUpperInvariant(), cancellationToken))
                throw new InvalidOperationException($"Another country with ISO3 code '{request.Iso3}' already exists");
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

        _context.AuditLogs.Add(new AuditLog { CountryId = country.Id, Action = "PATCH", UserId = userId, TimestampUtc = DateTime.UtcNow, Changes = JsonSerializer.Serialize(request) });
        await _context.SaveChangesAsync(cancellationToken);
        await InvalidateSingleCountryCacheAsync(country!, cancellationToken);
        await InvalidateListCachesAsync(cancellationToken);
        return MapToResponse(country);
    }

    /// <summary>
    /// Soft deletes a country.
    /// </summary>
    public async Task SoftDeleteAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var country = await _context.Countries.FindAsync(new object[] { id }, cancellationToken);
        if (country == null) throw new KeyNotFoundException($"Country with ID {id} not found");
        if (!country.IsActive) throw new InvalidOperationException($"Country {country.Iso2} is already inactive");

        country.IsActive = false;
        country.LastModifiedUtc = DateTime.UtcNow;
        country.DeletedAt = DateTime.UtcNow;
        country.UpdatedBy = userId;
        country.Version = Guid.NewGuid();

        _context.AuditLogs.Add(new AuditLog { CountryId = id, Action = "SOFT_DELETE", UserId = userId, TimestampUtc = DateTime.UtcNow, Changes = JsonSerializer.Serialize(new { IsActive = false, DeletedAt = country.DeletedAt }) });
        await _context.SaveChangesAsync(cancellationToken);
        await InvalidateSingleCountryCacheAsync(country!, cancellationToken);
        await InvalidateListCachesAsync(cancellationToken);
    }

    /// <summary>
    /// Hard deletes a country.
    /// </summary>
    public async Task HardDeleteAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var country = await _context.Countries.FindAsync(new object[] { id }, cancellationToken);
        if (country == null) throw new KeyNotFoundException($"Country with ID {id} not found");
        var iso2 = country.Iso2;
        var name = country.Name;

        _context.AuditLogs.Add(new AuditLog { CountryId = id, Action = "HARD_DELETE", UserId = userId, TimestampUtc = DateTime.UtcNow, Changes = JsonSerializer.Serialize(new { Id = id, Iso2 = iso2, Name = name }) });
        _context.Countries.Remove(country);
        await _context.SaveChangesAsync(cancellationToken);
        await InvalidateSingleCountryCacheAsync(country!, cancellationToken);
        await InvalidateListCachesAsync(cancellationToken);
    }

    /// <summary>
    /// Restores a soft-deleted country.
    /// </summary>
    public async Task RestoreAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var country = await _context.Countries.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (country == null) throw new KeyNotFoundException($"Country with ID {id} not found");
        if (country.IsActive) throw new InvalidOperationException($"Country {country.Iso2} is already active");

        country.IsActive = true;
        country.LastModifiedUtc = DateTime.UtcNow;
        country.DeletedAt = null;
        country.UpdatedBy = userId;
        country.Version = Guid.NewGuid();

        _context.AuditLogs.Add(new AuditLog { CountryId = id, Action = "RESTORE", UserId = userId, TimestampUtc = DateTime.UtcNow, Changes = JsonSerializer.Serialize(new { IsActive = true, RestoredAt = DateTime.UtcNow }) });
        await _context.SaveChangesAsync(cancellationToken);
        await InvalidateSingleCountryCacheAsync(country!, cancellationToken);
        await InvalidateListCachesAsync(cancellationToken);
    }

    private async Task InvalidateSingleCountryCacheAsync(Data.Entities.Country country, CancellationToken cancellationToken)
    {
        if (country == null) return;
        await _cacheService.RemoveAsync(GenerateCacheKey("id", country.Id.ToString()), cancellationToken);
        await _cacheService.RemoveAsync(GenerateCacheKey("iso2", country.Iso2), cancellationToken);
        if (!string.IsNullOrEmpty(country.Iso3)) await _cacheService.RemoveAsync(GenerateCacheKey("iso3", country.Iso3), cancellationToken);
    }
}
