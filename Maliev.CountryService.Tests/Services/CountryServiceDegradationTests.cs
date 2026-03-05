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
using Npgsql;
using System.Net.Sockets;

namespace Maliev.CountryService.Tests.Services;

[Collection("TestDatabase")]
public class CountryServiceDegradationTests
{
    private readonly TestWebApplicationFactory _factory;
    private readonly Mock<ILogger<AppCountryService>> _loggerMock;

    public CountryServiceDegradationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _loggerMock = new Mock<ILogger<AppCountryService>>();
    }

    private CountryDbContext GetDbContext() => _factory.GetDbContext();

    [Fact]
    public async Task GetByIdAsync_WhenDatabaseThrowsNpgsqlException_SetsDegradedAndThrows()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var mockCache = new Mock<ICacheService>();
        mockCache.Setup(x => x.GetAsync<CountryResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CountryResponse?)null);

        var degradationContext = new DegradationContext();
        var service = new AppCountryService(context, mockCache.Object, _loggerMock.Object, degradationContext);

        var country = new Country
        {
            Name = "Test",
            Iso2 = "TT",
            Iso3 = "TTT",
            CreatedBy = "user",
            UpdatedBy = "user"
        };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var dbContextMock = new Mock<ICountryDbContext>();
        dbContextMock.Setup(x => x.Countries).Throws(new NpgsqlException("Connection failed"));

        var serviceWithFailingDb = new AppCountryService(
            dbContextMock.Object, mockCache.Object, _loggerMock.Object, degradationContext);

        await Assert.ThrowsAsync<NpgsqlException>(() => serviceWithFailingDb.GetByIdAsync(country.Id));
        Assert.True(degradationContext.IsDegraded);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDatabaseThrowsSocketException_SetsDegradedAndThrows()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var mockCache = new Mock<ICacheService>();
        mockCache.Setup(x => x.GetAsync<CountryResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CountryResponse?)null);

        var degradationContext = new DegradationContext();

        var dbContextMock = new Mock<ICountryDbContext>();
        dbContextMock.Setup(x => x.Countries).Throws(new SocketException(10060));

        var service = new AppCountryService(
            dbContextMock.Object, mockCache.Object, _loggerMock.Object, degradationContext);

        await Assert.ThrowsAsync<SocketException>(() => service.GetByIdAsync(Guid.NewGuid()));
        Assert.True(degradationContext.IsDegraded);
    }

    [Fact]
    public async Task GetByIso2Async_WhenDatabaseFails_ReturnsStaleCacheData()
    {
        await _factory.CleanDatabaseAsync();

        var cachedResponse = new CountryResponse
        {
            Id = Guid.NewGuid(),
            Name = "Stale Cache",
            Iso2 = "SC"
        };

        var mockCache = new Mock<ICacheService>();

        mockCache.SetupSequence(x => x.GetAsync<CountryResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CountryResponse?)null)
            .ReturnsAsync(cachedResponse);

        var degradationContext = new DegradationContext();

        var dbContextMock = new Mock<ICountryDbContext>();
        dbContextMock.Setup(x => x.Countries).Throws(new NpgsqlException("Connection failed"));

        var service = new AppCountryService(
            dbContextMock.Object, mockCache.Object, _loggerMock.Object, degradationContext);

        var result = await service.GetByIso2Async("SC");

        Assert.NotNull(result);
        Assert.Equal("Stale Cache", result.Name);
        Assert.True(result.XServedFromCache);
        Assert.True(result.XCacheStale);
    }

    [Fact]
    public async Task GetByIso3Async_WhenDatabaseFails_ReturnsStaleCacheData()
    {
        await _factory.CleanDatabaseAsync();

        var cachedResponse = new CountryResponse
        {
            Id = Guid.NewGuid(),
            Name = "Stale Cache ISO3",
            Iso3 = "SCI"
        };

        var mockCache = new Mock<ICacheService>();

        mockCache.SetupSequence(x => x.GetAsync<CountryResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CountryResponse?)null)
            .ReturnsAsync(cachedResponse);

        var degradationContext = new DegradationContext();

        var dbContextMock = new Mock<ICountryDbContext>();
        dbContextMock.Setup(x => x.Countries).Throws(new NpgsqlException("Connection failed"));

        var service = new AppCountryService(
            dbContextMock.Object, mockCache.Object, _loggerMock.Object, degradationContext);

        var result = await service.GetByIso3Async("SCI");

        Assert.NotNull(result);
        Assert.Equal("Stale Cache ISO3", result.Name);
        Assert.True(result.XServedFromCache);
        Assert.True(result.XCacheStale);
    }

    [Fact]
    public async Task GetByIso2Async_WhenDatabaseFailsAndNoCache_ThrowsException()
    {
        await _factory.CleanDatabaseAsync();

        var mockCache = new Mock<ICacheService>();
        mockCache.Setup(x => x.GetAsync<CountryResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CountryResponse?)null);

        var degradationContext = new DegradationContext();

        var dbContextMock = new Mock<ICountryDbContext>();
        dbContextMock.Setup(x => x.Countries).Throws(new NpgsqlException("Connection failed"));

        var service = new AppCountryService(
            dbContextMock.Object, mockCache.Object, _loggerMock.Object, degradationContext);

        await Assert.ThrowsAsync<NpgsqlException>(() => service.GetByIso2Async("XX"));
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheHit_SetsXServedFromCache()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var cachedResponse = new CountryResponse
        {
            Id = Guid.NewGuid(),
            Name = "Cached",
            Iso2 = "CC"
        };

        var mockCache = new Mock<ICacheService>();
        mockCache.Setup(x => x.GetAsync<CountryResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var degradationContext = new DegradationContext();
        var service = new AppCountryService(context, mockCache.Object, _loggerMock.Object, degradationContext);

        var result = await service.GetByIdAsync(cachedResponse.Id);

        Assert.NotNull(result);
        Assert.True(result.XServedFromCache);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheHitAndDegraded_SetsXCacheStale()
    {
        await _factory.CleanDatabaseAsync();
        var context = GetDbContext();

        var cachedResponse = new CountryResponse
        {
            Id = Guid.NewGuid(),
            Name = "Cached",
            Iso2 = "CC"
        };

        var mockCache = new Mock<ICacheService>();
        mockCache.Setup(x => x.GetAsync<CountryResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var degradationContext = new DegradationContext { IsDegraded = true };
        var service = new AppCountryService(context, mockCache.Object, _loggerMock.Object, degradationContext);

        var result = await service.GetByIdAsync(cachedResponse.Id);

        Assert.NotNull(result);
        Assert.True(result.XServedFromCache);
        Assert.True(result.XCacheStale);
    }
}
