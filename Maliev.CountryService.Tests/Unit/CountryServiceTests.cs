using FluentAssertions;
using Maliev.CountryService.Api.Models;
using Maliev.CountryService.Api.Services;
using Maliev.CountryService.Data.DbContexts;
using Maliev.CountryService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Maliev.CountryService.Tests.Unit;

public class CountryServiceTests : IDisposable
{
    private readonly CountryDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<Api.Services.CountryService>> _loggerMock;
    private readonly IOptions<CacheOptions> _cacheOptions;
    private readonly Api.Services.CountryService _countryService;

    public CountryServiceTests()
    {
        var options = new DbContextOptionsBuilder<CountryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CountryDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<Api.Services.CountryService>>();
        
        _cacheOptions = Options.Create(new CacheOptions
        {
            CountryCacheDurationMinutes = 60,
            MaxCacheSize = 1000,
            SearchCacheDurationMinutes = 30
        });

        _countryService = new Api.Services.CountryService(_context, _cache, _cacheOptions, _loggerMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCountry_ReturnsCountry()
    {
        // Arrange
        var country = new Country
        {
            Id = 1,
            Name = "United States",
            Continent = "North America",
            ISO2 = "US",
            ISO3 = "USA",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _context.Countries.Add(country);
        await _context.SaveChangesAsync();

        // Add country code
        var countryCode = new CountryCode
        {
            CountryId = country.Id,
            Code = "1",
            IsPrimary = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        _context.CountryCodes.Add(countryCode);
        await _context.SaveChangesAsync();

        // Act
        var result = await _countryService.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("United States");
        result.Continent.Should().Be("North America");
        result.CountryCode.Should().Be("1");
        result.CountryCodes.Should().HaveCount(1);
        result.CountryCodes.First().Code.Should().Be("1");
        result.CountryCodes.First().IsPrimary.Should().BeTrue();
        result.ISO2.Should().Be("US");
        result.ISO3.Should().Be("USA");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingCountry_ReturnsNull()
    {
        // Act
        var result = await _countryService.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ValidCountry_CreatesAndReturnsCountry()
    {
        // Arrange
        var request = new CreateCountryRequest
        {
            Name = "Canada",
            Continent = "North America",
            CountryCode = "1",
            ISO2 = "CA",
            ISO3 = "CAN"
        };

        // Act
        var result = await _countryService.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Canada");
        result.Continent.Should().Be("North America");
        result.CountryCode.Should().Be("1");
        result.CountryCodes.Should().HaveCount(1);
        result.CountryCodes.First().Code.Should().Be("1");
        result.CountryCodes.First().IsPrimary.Should().BeTrue();
        result.ISO2.Should().Be("CA");
        result.ISO3.Should().Be("CAN");
        result.Id.Should().BeGreaterThan(0);

        var countryInDb = await _context.Countries.FirstOrDefaultAsync(c => c.Id == result.Id);
        countryInDb.Should().NotBeNull();
        countryInDb!.Name.Should().Be("Canada");
    }

    [Fact]
    public async Task UpdateAsync_ExistingCountry_UpdatesAndReturnsCountry()
    {
        // Arrange
        var country = new Country
        {
            Name = "United Kingdom",
            Continent = "Europe",
            ISO2 = "GB",
            ISO3 = "GBR",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _context.Countries.Add(country);
        await _context.SaveChangesAsync();

        // Add country code
        var countryCode = new CountryCode
        {
            CountryId = country.Id,
            Code = "44",
            IsPrimary = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        _context.CountryCodes.Add(countryCode);
        await _context.SaveChangesAsync();

        var request = new UpdateCountryRequest
        {
            Name = "United Kingdom of Great Britain",
            Continent = "Europe",
            CountryCode = "44",
            ISO2 = "GB",
            ISO3 = "GBR"
        };

        // Act
        var result = await _countryService.UpdateAsync(country.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("United Kingdom of Great Britain");
        result.Continent.Should().Be("Europe");

        var countryInDb = await _context.Countries.FirstOrDefaultAsync(c => c.Id == country.Id);
        countryInDb.Should().NotBeNull();
        countryInDb!.Name.Should().Be("United Kingdom of Great Britain");
    }

    [Fact]
    public async Task UpdateAsync_NonExistingCountry_ReturnsNull()
    {
        // Arrange
        var request = new UpdateCountryRequest
        {
            Name = "Non-existent Country",
            Continent = "Unknown",
            CountryCode = "999",
            ISO2 = "XX",
            ISO3 = "XXX"
        };

        // Act
        var result = await _countryService.UpdateAsync(999, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ExistingCountry_DeletesAndReturnsTrue()
    {
        // Arrange
        var country = new Country
        {
            Name = "Test Country",
            Continent = "Test Continent",
            ISO2 = "TC",
            ISO3 = "TST",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _context.Countries.Add(country);
        await _context.SaveChangesAsync();

        // Add country code
        var countryCode = new CountryCode
        {
            CountryId = country.Id,
            Code = "123",
            IsPrimary = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        _context.CountryCodes.Add(countryCode);
        await _context.SaveChangesAsync();

        // Act
        var result = await _countryService.DeleteAsync(country.Id);

        // Assert
        result.Should().BeTrue();

        var countryInDb = await _context.Countries.FirstOrDefaultAsync(c => c.Id == country.Id);
        countryInDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingCountry_ReturnsFalse()
    {
        // Act
        var result = await _countryService.DeleteAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByNameAsync_ExistingName_ReturnsTrue()
    {
        // Arrange
        var country = new Country
        {
            Name = "France",
            Continent = "Europe",
            ISO2 = "FR",
            ISO3 = "FRA",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _context.Countries.Add(country);
        await _context.SaveChangesAsync();

        // Add country code
        var countryCode = new CountryCode
        {
            CountryId = country.Id,
            Code = "33",
            IsPrimary = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        _context.CountryCodes.Add(countryCode);
        await _context.SaveChangesAsync();

        // Act
        var result = await _countryService.ExistsByNameAsync("France");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByNameAsync_NonExistingName_ReturnsFalse()
    {
        // Act
        var result = await _countryService.ExistsByNameAsync("Non-existent Country");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SearchAsync_WithNameFilter_ReturnsFilteredResults()
    {
        // Arrange
        var countries = new[]
        {
            new Country { Name = "United States", Continent = "North America", ISO2 = "US", ISO3 = "USA", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new Country { Name = "United Kingdom", Continent = "Europe", ISO2 = "GB", ISO3 = "GBR", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new Country { Name = "Canada", Continent = "North America", ISO2 = "CA", ISO3 = "CAN", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow }
        };

        _context.Countries.AddRange(countries);
        await _context.SaveChangesAsync();

        // Add country codes
        var countryCodes = new[]
        {
            new CountryCode { CountryId = countries[0].Id, Code = "1", IsPrimary = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new CountryCode { CountryId = countries[1].Id, Code = "44", IsPrimary = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new CountryCode { CountryId = countries[2].Id, Code = "1", IsPrimary = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow }
        };
        _context.CountryCodes.AddRange(countryCodes);
        await _context.SaveChangesAsync();

        var searchRequest = new CountrySearchRequest
        {
            Name = "United",
            PageSize = 10,
            PageNumber = 1
        };

        // Act
        var result = await _countryService.SearchAsync(searchRequest);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(c => c.Name.Contains("United"));
    }

    [Fact]
    public async Task SearchAsync_WithContinentFilter_ReturnsFilteredResults()
    {
        // Arrange
        var countries = new[]
        {
            new Country { Name = "United States", Continent = "North America", ISO2 = "US", ISO3 = "USA", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new Country { Name = "Canada", Continent = "North America", ISO2 = "CA", ISO3 = "CAN", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new Country { Name = "France", Continent = "Europe", ISO2 = "FR", ISO3 = "FRA", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow }
        };

        _context.Countries.AddRange(countries);
        await _context.SaveChangesAsync();

        // Add country codes
        var countryCodes = new[]
        {
            new CountryCode { CountryId = countries[0].Id, Code = "1", IsPrimary = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new CountryCode { CountryId = countries[1].Id, Code = "1", IsPrimary = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new CountryCode { CountryId = countries[2].Id, Code = "33", IsPrimary = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow }
        };
        _context.CountryCodes.AddRange(countryCodes);
        await _context.SaveChangesAsync();

        var searchRequest = new CountrySearchRequest
        {
            Continent = "North America",
            PageSize = 10,
            PageNumber = 1
        };

        // Act
        var result = await _countryService.SearchAsync(searchRequest);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(c => c.Continent == "North America");
    }

    [Fact]
    public async Task CreateAsync_CountryWithMultipleCountryCodes_CreatesAndReturnsCountryWithAllCodes()
    {
        // Arrange
        var request = new CreateCountryRequest
        {
            Name = "Dominican Republic",
            Continent = "North America",
            CountryCode = "1-809, 1-829, 1-849",
            ISO2 = "DO",
            ISO3 = "DOM"
        };

        // Act
        var result = await _countryService.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Dominican Republic");
        result.Continent.Should().Be("North America");
        result.CountryCode.Should().Be("1-809"); // Primary code should be first
        result.CountryCodes.Should().HaveCount(3);
        
        var codes = result.CountryCodes.OrderBy(c => c.Code).ToList();
        codes[0].Code.Should().Be("1-809");
        codes[0].IsPrimary.Should().BeTrue();
        codes[1].Code.Should().Be("1-829");
        codes[1].IsPrimary.Should().BeFalse();
        codes[2].Code.Should().Be("1-849");
        codes[2].IsPrimary.Should().BeFalse();
        
        result.ISO2.Should().Be("DO");
        result.ISO3.Should().Be("DOM");
        result.Id.Should().BeGreaterThan(0);

        // Verify database state
        var countryInDb = await _context.Countries
            .Include(c => c.CountryCodes)
            .FirstOrDefaultAsync(c => c.Id == result.Id);
        countryInDb.Should().NotBeNull();
        countryInDb!.CountryCodes.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetContinentsAsync_WithCountries_ReturnsUniqueContinents()
    {
        // Arrange
        var countries = new[]
        {
            new Country { Name = "United States", Continent = "North America", ISO2 = "US", ISO3 = "USA", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new Country { Name = "Canada", Continent = "North America", ISO2 = "CA", ISO3 = "CAN", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new Country { Name = "France", Continent = "Europe", ISO2 = "FR", ISO3 = "FRA", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new Country { Name = "Germany", Continent = "Europe", ISO2 = "DE", ISO3 = "DEU", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow }
        };

        _context.Countries.AddRange(countries);
        await _context.SaveChangesAsync();

        // Add country codes
        var countryCodes = new[]
        {
            new CountryCode { CountryId = countries[0].Id, Code = "1", IsPrimary = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new CountryCode { CountryId = countries[1].Id, Code = "1", IsPrimary = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new CountryCode { CountryId = countries[2].Id, Code = "33", IsPrimary = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new CountryCode { CountryId = countries[3].Id, Code = "49", IsPrimary = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow }
        };
        _context.CountryCodes.AddRange(countryCodes);
        await _context.SaveChangesAsync();

        // Act
        var result = await _countryService.GetContinentsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain("North America");
        result.Should().Contain("Europe");
        result.Should().BeInAscendingOrder();
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
    }
}