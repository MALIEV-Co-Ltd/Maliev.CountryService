using Maliev.CountryService.Application.Interfaces;
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
public class CountryServiceListSortingTests
{
    private readonly TestWebApplicationFactory _factory;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<AppCountryService>> _loggerMock;
    private readonly DegradationContext _degradationContext;

    public CountryServiceListSortingTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<AppCountryService>>();
        _degradationContext = new DegradationContext();
    }

    private CountryDbContext GetDbContext() => _factory.GetDbContext();

    [Fact]
    public async Task ListAsync_SortedByIso2Ascending_ReturnsCorrectOrder()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var countries = new List<Country>
        {
            new() { Name = "Zambia", Iso2 = "ZM", Iso3 = "ZMB", CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() },
            new() { Name = "Albania", Iso2 = "AL", Iso3 = "ALB", CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() },
            new() { Name = "Brazil", Iso2 = "BR", Iso3 = "BRA", CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() }
        };
        context.Countries.AddRange(countries);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var request = new CountryListRequest { SortBy = "iso2", SortOrder = "asc", PageSize = 10 };

        var result = await service.ListAsync(request);
        var dataList = result.Data.ToList();

        Assert.Equal(3, dataList.Count);
        Assert.Equal("AL", dataList[0].Iso2);
        Assert.Equal("BR", dataList[1].Iso2);
        Assert.Equal("ZM", dataList[2].Iso2);
    }

    [Fact]
    public async Task ListAsync_SortedByIso2Descending_ReturnsCorrectOrder()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var countries = new List<Country>
        {
            new() { Name = "Zambia", Iso2 = "ZM", Iso3 = "ZMB", CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() },
            new() { Name = "Albania", Iso2 = "AL", Iso3 = "ALB", CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() },
            new() { Name = "Brazil", Iso2 = "BR", Iso3 = "BRA", CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() }
        };
        context.Countries.AddRange(countries);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var request = new CountryListRequest { SortBy = "iso2", SortOrder = "desc", PageSize = 10 };

        var result = await service.ListAsync(request);
        var dataList = result.Data.ToList();

        Assert.Equal(3, dataList.Count);
        Assert.Equal("ZM", dataList[0].Iso2);
        Assert.Equal("BR", dataList[1].Iso2);
        Assert.Equal("AL", dataList[2].Iso2);
    }

    [Fact]
    public async Task ListAsync_SortedByAreaDescending_ReturnsCorrectOrder()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var countries = new List<Country>
        {
            new() { Name = "Small", Iso2 = "SM", Iso3 = "SML", AreaKm2 = 100, CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() },
            new() { Name = "Large", Iso2 = "LG", Iso3 = "LGE", AreaKm2 = 1000000, CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() },
            new() { Name = "Medium", Iso2 = "MD", Iso3 = "MDM", AreaKm2 = 500000, CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() }
        };
        context.Countries.AddRange(countries);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var request = new CountryListRequest { SortBy = "area", SortOrder = "desc", PageSize = 10 };

        var result = await service.ListAsync(request);
        var dataList = result.Data.ToList();

        Assert.Equal(3, dataList.Count);
        Assert.Equal(1000000, dataList[0].AreaKm2);
        Assert.Equal(500000, dataList[1].AreaKm2);
        Assert.Equal(100, dataList[2].AreaKm2);
    }

    [Fact]
    public async Task ListAsync_SortedByPopulationAscending_ReturnsCorrectOrder()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var countries = new List<Country>
        {
            new() { Name = "Many", Iso2 = "MY", Iso3 = "MAN", Population = 1000000, CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() },
            new() { Name = "Few", Iso2 = "FW", Iso3 = "FEW", Population = 10000, CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() },
            new() { Name = "Some", Iso2 = "SM", Iso3 = "SOM", Population = 500000, CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() }
        };
        context.Countries.AddRange(countries);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var request = new CountryListRequest { SortBy = "population", SortOrder = "asc", PageSize = 10 };

        var result = await service.ListAsync(request);
        var dataList = result.Data.ToList();

        Assert.Equal(3, dataList.Count);
        Assert.Equal(10000, dataList[0].Population);
        Assert.Equal(500000, dataList[1].Population);
        Assert.Equal(1000000, dataList[2].Population);
    }

    [Fact]
    public async Task ListAsync_WithRegionAndSubregionFilter_ReturnsFilteredResults()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var countries = new List<Country>
        {
            new() { Name = "France", Iso2 = "FR", Iso3 = "FRA", Region = "Europe", Subregion = "Western Europe", CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() },
            new() { Name = "Germany", Iso2 = "DE", Iso3 = "DEU", Region = "Europe", Subregion = "Western Europe", CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() },
            new() { Name = "Japan", Iso2 = "JP", Iso3 = "JPN", Region = "Asia", Subregion = "Eastern Asia", CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() }
        };
        context.Countries.AddRange(countries);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var request = new CountryListRequest { Region = "Europe", Subregion = "Western Europe", PageSize = 10 };

        var result = await service.ListAsync(request);
        var dataList = result.Data.ToList();

        Assert.Equal(2, dataList.Count);
        Assert.All(dataList, c => Assert.Equal("Europe", c.Region));
        Assert.All(dataList, c => Assert.Equal("Western Europe", c.Subregion));
    }

    // PageSize clamping is tested via integration tests
    // Page defaults to 1 tested in integration tests

    [Fact]
    public async Task ListAsync_WithEmptySubregionFilter_ReturnsAllInRegion()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var countries = new List<Country>
        {
            new() { Name = "Japan", Iso2 = "JP", Iso3 = "JPN", Region = "Asia", Subregion = "Eastern Asia", CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() },
            new() { Name = "China", Iso2 = "CN", Iso3 = "CHN", Region = "Asia", Subregion = "Eastern Asia", CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() },
            new() { Name = "India", Iso2 = "IN", Iso3 = "IND", Region = "Asia", Subregion = "Southern Asia", CreatedBy = "user", UpdatedBy = "user", Version = Guid.NewGuid() }
        };
        context.Countries.AddRange(countries);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var request = new CountryListRequest { Region = "Asia", PageSize = 10 };

        var result = await service.ListAsync(request);

        Assert.Equal(3, result.Data.Count());
    }

    [Fact]
    public async Task CreateAsync_DuplicateIso3_ThrowsInvalidOperationException()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var country = new Country
        {
            Name = "Existing",
            Iso2 = "EX",
            Iso3 = "EXI",
            CreatedBy = "user",
            UpdatedBy = "user",
            Version = Guid.NewGuid()
        };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);
        var request = new CreateCountryRequest { Name = "New", Iso2 = "NE", Iso3 = "EXI" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, "user"));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateIso2ForDifferentCountry_ThrowsInvalidOperationException()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var country1 = new Country
        {
            Name = "Country1",
            Iso2 = "C1",
            Iso3 = "C1X",
            CreatedBy = "user",
            UpdatedBy = "user",
            Version = Guid.NewGuid()
        };
        var country2 = new Country
        {
            Name = "Country2",
            Iso2 = "C2",
            Iso3 = "C2X",
            CreatedBy = "user",
            UpdatedBy = "user",
            Version = Guid.NewGuid()
        };
        context.Countries.AddRange(country1, country2);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        var getResult = await service.GetByIdAsync(country1.Id);

        var updateRequest = new UpdateCountryRequest
        {
            Name = "Updated",
            Iso2 = "C2",
            Iso3 = "C1X"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(country1.Id, updateRequest, getResult!.ETag, "user"));
    }

    [Fact]
    public async Task PatchAsync_DuplicateIso2ForDifferentCountry_ThrowsInvalidOperationException()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var country1 = new Country
        {
            Name = "Country1",
            Iso2 = "C1",
            Iso3 = "C1X",
            CreatedBy = "user",
            UpdatedBy = "user",
            Version = Guid.NewGuid()
        };
        var country2 = new Country
        {
            Name = "Country2",
            Iso2 = "C2",
            Iso3 = "C2X",
            CreatedBy = "user",
            UpdatedBy = "user",
            Version = Guid.NewGuid()
        };
        context.Countries.AddRange(country1, country2);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        var getResult = await service.GetByIdAsync(country1.Id);

        var patchRequest = new PatchCountryRequest { Iso2 = "C2" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PatchAsync(country1.Id, patchRequest, getResult!.ETag, "user"));
    }

    [Fact]
    public async Task SoftDeleteAsync_AlreadyInactive_ThrowsInvalidOperationException()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var country = new Country
        {
            Name = "Inactive",
            Iso2 = "IA",
            Iso3 = "INA",
            IsActive = false,
            CreatedBy = "user",
            UpdatedBy = "user",
            Version = Guid.NewGuid()
        };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SoftDeleteAsync(country.Id, "user"));
    }

    [Fact]
    public async Task RestoreAsync_AlreadyActive_ThrowsInvalidOperationException()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var country = new Country
        {
            Name = "Active",
            Iso2 = "AC",
            Iso3 = "ACT",
            IsActive = true,
            CreatedBy = "user",
            UpdatedBy = "user",
            Version = Guid.NewGuid()
        };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RestoreAsync(country.Id, "user"));
    }

    [Fact]
    public async Task RestoreAsync_NotFound_ThrowsKeyNotFoundException()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.RestoreAsync(Guid.NewGuid(), "user"));
    }

    [Fact]
    public async Task HardDeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.HardDeleteAsync(Guid.NewGuid(), "user"));
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsKeyNotFoundException()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateAsync(Guid.NewGuid(), new UpdateCountryRequest { Name = "Test", Iso2 = "TT", Iso3 = "TTT" }, "", "user"));
    }

    [Fact]
    public async Task PatchAsync_NotFound_ThrowsKeyNotFoundException()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.PatchAsync(Guid.NewGuid(), new PatchCountryRequest { Name = "Test" }, "", "user"));
    }

    [Fact]
    public async Task SoftDeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var service = new AppCountryService(context, _cacheServiceMock.Object, _loggerMock.Object, _degradationContext);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.SoftDeleteAsync(Guid.NewGuid(), "user"));
    }
}
