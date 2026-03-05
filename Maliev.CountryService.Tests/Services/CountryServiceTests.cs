using Maliev.CountryService.Application.Interfaces;
using Maliev.CountryService.Application.Models.Common;
using Maliev.CountryService.Application.Models.Countries;
using Maliev.CountryService.Domain.Entities;
using Maliev.CountryService.Infrastructure.Data;
using Maliev.CountryService.Infrastructure.Services;
using Maliev.CountryService.Tests.Fixtures;
using AppCountryService = Maliev.CountryService.Application.Services.CountryService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.CountryService.Tests.Services;

[Collection("TestDatabase")]
public class CountryServiceTests
{
    private readonly TestWebApplicationFactory _factory;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<AppCountryService>> _loggerMock;
    private readonly DegradationContext _degradationContext;

    public CountryServiceTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<AppCountryService>>();
        _degradationContext = new DegradationContext();
    }

    private CountryDbContext GetDbContext() => _factory.GetDbContext();

    [Fact]
    public async Task GetByIdAsync_ReturnsFromCache_IfAvailable()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cachedResponse = new CountryResponse { Id = id, Name = "Cached", Iso2 = "CH" };
        _cacheServiceMock.Setup(x => x.GetAsync<CountryResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var context = GetDbContext();
        var service = new Maliev.CountryService.Application.Services.CountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act
        var result = await service.GetByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Cached", result.Name);
        Assert.True(result.XServedFromCache);
    }

    [Fact]
    public async Task CreateAsync_SavesToDbAndInvalidatesCache()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var service = new Maliev.CountryService.Application.Services.CountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var request = new CreateCountryRequest { Name = "New Country", Iso2 = "NC", Iso3 = "NCU", NumericCode = "999" };

        // Act
        var result = await service.CreateAsync(request, "test-user");

        // Assert
        Assert.NotNull(result);
        var inDb = await context.Countries.FirstOrDefaultAsync(c => c.Iso2 == "NC");
        Assert.NotNull(inDb);
        _cacheServiceMock.Verify(x => x.RemovePatternAsync(It.Is<string>(s => s.Contains("country:list:")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteAsync_SetsIsActiveFalse()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var country = new Country { Name = "Active", Iso2 = "AC", Iso3 = "ACT", CreatedBy = "user", UpdatedBy = "user", IsActive = true };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var service = new Maliev.CountryService.Application.Services.CountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act
        await service.SoftDeleteAsync(country.Id, "test-user");

        // Assert
        var updated = await context.Countries.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == country.Id);
        Assert.False(updated!.IsActive);
        Assert.NotNull(updated.DeletedAt);
    }

    [Fact]
    public async Task GetByIso2Async_ReturnsNull_IfNotFound()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var service = new Maliev.CountryService.Application.Services.CountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act
        var result = await service.GetByIso2Async("XX");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIso3Async_ReturnsFromCache_IfAvailable()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cachedResponse = new CountryResponse { Id = id, Name = "Cached", Iso3 = "USA" };
        _cacheServiceMock.Setup(x => x.GetAsync<CountryResponse>(It.Is<string>(k => k.Contains("iso3:USA")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var context = GetDbContext();
        var service = new Maliev.CountryService.Application.Services.CountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act
        var result = await service.GetByIso3Async("USA");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Cached", result.Name);
    }

    [Fact]
    public async Task ListAsync_IncludesInactive_WhenRequested()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var country = new Country { Name = "00-Inactive", Iso2 = "IA", Iso3 = "IAT", CreatedBy = "user", UpdatedBy = "user", IsActive = false };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var service = new Maliev.CountryService.Application.Services.CountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var request = new CountryListRequest { IncludeInactive = true, PageSize = 1000 };

        // Act
        var result = await service.ListAsync(request);

        // Assert
        Assert.Contains(result.Data, c => c.Iso2 == "IA");
    }

    [Fact]
    public async Task SearchAsync_MatchesOfficialName()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var country = new Country { Name = "France", OfficialName = "French Republic", Iso2 = "FR", Iso3 = "FRA", CreatedBy = "user", UpdatedBy = "user", IsActive = true };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var service = new Maliev.CountryService.Application.Services.CountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act
        var result = await service.SearchAsync("Republic", 1, 10);

        // Assert
        Assert.Single(result.Data);
        Assert.Equal("France", result.Data.First().Name);
    }

    [Fact]
    public async Task SearchAsync_InvalidQuery_ReturnsEmpty()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var service = new Maliev.CountryService.Application.Services.CountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act
        var result1 = await service.SearchAsync("", 1, 10);
        var result2 = await service.SearchAsync("a", 1, 10);

        // Assert
        Assert.Empty(result1.Data);
        Assert.Empty(result2.Data);
    }

    [Fact]
    public async Task RestoreAsync_RestoresCountry()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var country = new Country { Name = "Inactive", Iso2 = "IA", Iso3 = "IAT", CreatedBy = "user", UpdatedBy = "user", IsActive = false, DeletedAt = DateTime.UtcNow };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var service = new Maliev.CountryService.Application.Services.CountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act
        await service.RestoreAsync(country.Id, "test-user");

        // Assert
        var updated = await context.Countries.FirstOrDefaultAsync(c => c.Id == country.Id);
        Assert.True(updated!.IsActive);
        Assert.Null(updated.DeletedAt);
    }

    [Fact]
    public async Task PatchAsync_UpdatesOnlySpecifiedFields()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var country = new Country { Name = "Original", Region = "Old", Iso2 = "OR", Iso3 = "ORG", CreatedBy = "user", UpdatedBy = "user", IsActive = true };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var service = new Maliev.CountryService.Application.Services.CountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var patch = new PatchCountryRequest { Region = "New" };

        var countryResponse = await service.GetByIdAsync(country.Id);

        // Act
        var result = await service.PatchAsync(country.Id, patch, countryResponse!.ETag, "test-user");

        // Assert
        Assert.Equal("Original", result.Name);
        Assert.Equal("New", result.Region);
    }

    [Fact]
    public async Task HardDeleteAsync_RemovesFromDb()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var country = new Country { Name = "To Delete", Iso2 = "TD", Iso3 = "TDE", CreatedBy = "user", UpdatedBy = "user", IsActive = true };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act
        await service.HardDeleteAsync(country.Id, "test-user");

        // Assert
        var inDb = await context.Countries.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == country.Id);
        Assert.Null(inDb);
    }

    [Fact]
    public async Task UpdateAsync_ConcurrencyConflict_ThrowsInvalidOperationException()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var country = new Country { Name = "C1", Iso2 = "C1", Iso3 = "C11", CreatedBy = "user", UpdatedBy = "user", IsActive = true };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var update = new UpdateCountryRequest { Name = "C2", Iso2 = "C1", Iso3 = "C11" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(country.Id, update, "\"wrong-etag\"", "user"));
    }

    [Fact]
    public async Task CreateAsync_DuplicateIso2_ThrowsInvalidOperationException()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var country = new Country { Name = "C1", Iso2 = "C1", Iso3 = "C11", CreatedBy = "user", UpdatedBy = "user", IsActive = true };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var request = new CreateCountryRequest { Name = "C2", Iso2 = "C1", Iso3 = "C22" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, "user"));
    }

    [Fact]
    public async Task GetByIdAsync_MapsAllFieldsCorrectly()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var country = new Country
        {
            Name = "Full Test",
            Iso2 = "FT",
            Iso3 = "FTT",
            OfficialName = "Official Full Test",
            NumericCode = "123",
            Capital = "Capital",
            Region = "Region",
            Subregion = "Subregion",
            Latitude = 1.0,
            Longitude = 2.0,
            Demonym = "Testian",
            AreaKm2 = 1000.0,
            Population = 1000000,
            GiniCoefficient = 30.0,
            Timezones = "[\"UTC\"]",
            Borders = "[\"ABC\"]",
            CallingCodes = "[\"+1\"]",
            TopLevelDomains = "[\".ft\"]",
            Currencies = "{\"USD\":{\"name\":\"Dollar\"}}",
            Languages = "{\"eng\":\"English\"}",
            Translations = "{\"fra\":{\"official\":\"...\"}}",
            Flags = "{\"png\":\"...\"}",
            CoatOfArms = "{\"png\":\"...\"}",
            Independent = true,
            UnMember = true,
            Landlocked = false,
            IsActive = true,
            CreatedBy = "user",
            UpdatedBy = "user"
        };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act
        var result = await service.GetByIdAsync(country.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(country.Name, result.Name);
        Assert.Equal(country.Iso2, result.Iso2);
        Assert.Equal(country.Iso3, result.Iso3);
        Assert.Equal(country.OfficialName, result.OfficialName);
        Assert.Equal(country.NumericCode, result.NumericCode);
        Assert.Equal(country.Capital, result.Capital);
        Assert.Equal(country.Region, result.Region);
        Assert.Equal(country.Subregion, result.Subregion);
        Assert.Equal(country.Latitude, result.Latitude);
        Assert.Equal(country.Longitude, result.Longitude);
        Assert.Equal(country.Demonym, result.Demonym);
        Assert.Equal(country.AreaKm2, result.AreaKm2);
        Assert.Equal(country.Population, result.Population);
        Assert.Equal(country.GiniCoefficient, result.GiniCoefficient);
        Assert.Equal(country.Independent, result.Independent);
        Assert.Equal(country.UnMember, result.UnMember);
        Assert.Equal(country.Landlocked, result.Landlocked);
        Assert.NotNull(result.ETag);
    }

    [Fact]
    public async Task CreateAsync_DuplicateIso3_ThrowsInvalidOperationException()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var country = new Country { Name = "C1", Iso2 = "C1", Iso3 = "C11", CreatedBy = "user", UpdatedBy = "user", IsActive = true };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var request = new CreateCountryRequest { Name = "C2", Iso2 = "C2", Iso3 = "C11" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, "user"));
    }

    [Fact]
    public async Task SoftDeleteAsync_AlreadyInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var country = new Country { Name = "Inactive", Iso2 = "IN", Iso3 = "INA", CreatedBy = "user", UpdatedBy = "user", IsActive = false };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SoftDeleteAsync(country.Id, "test-user"));
    }

    [Fact]
    public async Task RestoreAsync_AlreadyActive_ThrowsInvalidOperationException()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var country = new Country { Name = "Active", Iso2 = "AC", Iso3 = "ACT", CreatedBy = "user", UpdatedBy = "user", IsActive = true };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RestoreAsync(country.Id, "test-user"));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateIso2_ThrowsInvalidOperationException()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var country1 = new Country { Name = "C1", Iso2 = "C1", Iso3 = "C11", CreatedBy = "user", UpdatedBy = "user", IsActive = true };
        var country2 = new Country { Name = "C2", Iso2 = "C2", Iso3 = "C22", CreatedBy = "user", UpdatedBy = "user", IsActive = true };
        context.Countries.AddRange(country1, country2);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var response = await service.GetByIdAsync(country2.Id);
        var update = new UpdateCountryRequest { Name = "Updated", Iso2 = "C1", Iso3 = "C22" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(country2.Id, update, response!.ETag, "user"));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateIso3_ThrowsInvalidOperationException()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var country1 = new Country { Name = "C1", Iso2 = "C1", Iso3 = "C11", CreatedBy = "user", UpdatedBy = "user", IsActive = true };
        var country2 = new Country { Name = "C2", Iso2 = "C2", Iso3 = "C22", CreatedBy = "user", UpdatedBy = "user", IsActive = true };
        context.Countries.AddRange(country1, country2);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var response = await service.GetByIdAsync(country2.Id);
        var update = new UpdateCountryRequest { Name = "Updated", Iso2 = "C2", Iso3 = "C11" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(country2.Id, update, response!.ETag, "user"));
    }

    [Fact]
    public async Task PatchAsync_DuplicateIso2_ThrowsInvalidOperationException()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var country1 = new Country { Name = "C1", Iso2 = "C1", Iso3 = "C11", CreatedBy = "user", UpdatedBy = "user", IsActive = true };
        var country2 = new Country { Name = "C2", Iso2 = "C2", Iso3 = "C22", CreatedBy = "user", UpdatedBy = "user", IsActive = true };
        context.Countries.AddRange(country1, country2);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var response = await service.GetByIdAsync(country2.Id);
        var patch = new PatchCountryRequest { Iso2 = "C1" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PatchAsync(country2.Id, patch, response!.ETag, "user"));
    }

    [Fact]
    public async Task PatchAsync_DuplicateIso3_ThrowsInvalidOperationException()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var country1 = new Country { Name = "C1", Iso2 = "C1", Iso3 = "C11", CreatedBy = "user", UpdatedBy = "user", IsActive = true };
        var country2 = new Country { Name = "C2", Iso2 = "C2", Iso3 = "C22", CreatedBy = "user", UpdatedBy = "user", IsActive = true };
        context.Countries.AddRange(country1, country2);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var response = await service.GetByIdAsync(country2.Id);
        var patch = new PatchCountryRequest { Iso3 = "C11" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PatchAsync(country2.Id, patch, response!.ETag, "user"));
    }

    [Fact]
    public async Task SearchAsync_ReturnsFromCache_IfAvailable()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var cachedResponse = new PaginatedResponse<CountryResponse> { Data = new List<CountryResponse> { new() { Name = "Cached" } } };
        _cacheServiceMock.Setup(x => x.GetAsync<PaginatedResponse<CountryResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act
        var result = await service.SearchAsync("test", 1, 10);

        // Assert
        Assert.Single(result.Data);
        Assert.Equal("Cached", result.Data.First().Name);
    }

    [Fact]
    public async Task ListAsync_ReturnsFromCache_IfAvailable()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var cachedResponse = new PaginatedResponse<CountryResponse> { Data = new List<CountryResponse> { new() { Name = "CachedList" } }, TotalCount = 1 };
        _cacheServiceMock.Setup(x => x.GetAsync<PaginatedResponse<CountryResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act
        var result = await service.ListAsync(new CountryListRequest());

        // Assert
        Assert.Single(result.Data);
        Assert.Equal("CachedList", result.Data.First().Name);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var update = new UpdateCountryRequest { Name = "Updated", Iso2 = "XX", Iso3 = "XXX" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(Guid.NewGuid(), update, "", "user"));
    }

    [Fact]
    public async Task PatchAsync_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var patch = new PatchCountryRequest { Name = "Patched" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.PatchAsync(Guid.NewGuid(), patch, "", "user"));
    }

    [Fact]
    public async Task SoftDeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SoftDeleteAsync(Guid.NewGuid(), "user"));
    }

    [Fact]
    public async Task HardDeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.HardDeleteAsync(Guid.NewGuid(), "user"));
    }

    [Fact]
    public async Task RestoreAsync_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RestoreAsync(Guid.NewGuid(), "user"));
    }

    [Fact]
    public async Task ListAsync_WithRegionFilter_ReturnsFiltered()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        context.Countries.Add(new Country { Name = "Asia Country", Region = "Asia", Iso2 = "AC", Iso3 = "ACT", CreatedBy = "user", UpdatedBy = "user" });
        context.Countries.Add(new Country { Name = "Europe Country", Region = "Europe", Iso2 = "EC", Iso3 = "ECT", CreatedBy = "user", UpdatedBy = "user" });
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act
        var result = await service.ListAsync(new CountryListRequest { Region = "Asia" });

        // Assert
        Assert.Single(result.Data);
        Assert.Equal("Asia Country", result.Data.First().Name);
    }

    [Fact]
    public async Task ListAsync_WithSubregionFilter_ReturnsFiltered()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        context.Countries.Add(new Country { Name = "SE Asia", Region = "Asia", Subregion = "Southeast Asia", Iso2 = "SA", Iso3 = "SEA", CreatedBy = "user", UpdatedBy = "user" });
        context.Countries.Add(new Country { Name = "E Asia", Region = "Asia", Subregion = "East Asia", Iso2 = "EA", Iso3 = "EAS", CreatedBy = "user", UpdatedBy = "user" });
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act
        var result = await service.ListAsync(new CountryListRequest { Subregion = "Southeast Asia" });

        // Assert
        Assert.Single(result.Data);
        Assert.Equal("SE Asia", result.Data.First().Name);
    }

    [Fact]
    public async Task ListAsync_WithSorting_SortsCorrectly()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        context.Countries.Add(new Country { Name = "Zebra", Population = 1000, Iso2 = "ZA", Iso3 = "ZAB", CreatedBy = "user", UpdatedBy = "user" });
        context.Countries.Add(new Country { Name = "Alpha", Population = 5000, Iso2 = "AA", Iso3 = "AAB", CreatedBy = "user", UpdatedBy = "user" });
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act - Sort by name desc
        var resultDesc = await service.ListAsync(new CountryListRequest { SortBy = "name", SortOrder = "desc" });

        // Assert
        Assert.Equal("Zebra", resultDesc.Data.First().Name);
    }

    [Fact]
    public async Task ListAsync_SortByIso2_SortsCorrectly()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        context.Countries.Add(new Country { Name = "Zeta", Iso2 = "ZZ", Iso3 = "ZZZ", CreatedBy = "user", UpdatedBy = "user" });
        context.Countries.Add(new Country { Name = "Alpha", Iso2 = "AA", Iso3 = "AAA", CreatedBy = "user", UpdatedBy = "user" });
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act
        var result = await service.ListAsync(new CountryListRequest { SortBy = "iso2", SortOrder = "asc" });

        // Assert
        Assert.Equal("Alpha", result.Data.First().Name);
    }

    [Fact]
    public async Task InvalidateListCachesAsync_CallsCacheService()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();
        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        // Act
        await service.InvalidateListCachesAsync();

        // Assert
        _cacheServiceMock.Verify(x => x.RemovePatternAsync("country:list:*", It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(x => x.RemovePatternAsync("country:search:*", It.IsAny<CancellationToken>()), Times.Once);
    }
}
