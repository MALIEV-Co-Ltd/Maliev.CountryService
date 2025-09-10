using FluentAssertions;
using Maliev.CountryService.Api.Models;
using Maliev.CountryService.Data.DbContexts;
using Maliev.CountryService.Data.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Maliev.CountryService.Tests.Integration;

public class CountriesControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly CountryDbContext _context;

    public CountriesControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<CountryDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add InMemory database for testing
                services.AddDbContext<CountryDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });

                // Reduce logging noise during tests
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.SetMinimumLevel(LogLevel.Warning);
                });

                // Authentication is disabled in Testing environment
            });
        });

        _client = _factory.CreateClient();
        
        // Get the test database context
        using var scope = _factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<CountryDbContext>();
        _context.Database.EnsureCreated();

        // Authentication is handled by TestAuthenticationSchemeHandler
    }

    [Fact]
    public async Task GetById_ExistingCountry_ReturnsOk()
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

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CountryDbContext>();
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        // Add country code
        var countryCode = new CountryCode
        {
            CountryId = country.Id,
            Code = "123",
            IsPrimary = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        context.CountryCodes.Add(countryCode);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/countries/v1.0/{country.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CountryDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Id.Should().Be(country.Id);
        result.Name.Should().Be("Test Country");
    }

    [Fact]
    public async Task GetById_NonExistingCountry_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/countries/v1.0/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_WithValidParameters_ReturnsOk()
    {
        // Arrange
        var countries = new[]
        {
            new Country { Name = "United States", Continent = "North America", ISO2 = "US", ISO3 = "USA", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new Country { Name = "Canada", Continent = "North America", ISO2 = "CA", ISO3 = "CAN", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow }
        };

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CountryDbContext>();
        context.Countries.AddRange(countries);
        await context.SaveChangesAsync();

        // Add country codes
        var countryCodes = new[]
        {
            new CountryCode { CountryId = countries[0].Id, Code = "1", IsPrimary = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new CountryCode { CountryId = countries[1].Id, Code = "1", IsPrimary = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow }
        };
        context.CountryCodes.AddRange(countryCodes);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/countries/v1.0/search?continent=North America&pageSize=10&pageNumber=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<CountryDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.TotalCount.Should().BeGreaterThan(0);
        result.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_ValidCountry_ReturnsCreated()
    {
        // Arrange
        var request = new CreateCountryRequest
        {
            Name = "New Country",
            Continent = "Test Continent",
            CountryCode = "456",
            ISO2 = "NC",
            ISO3 = "NEW"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/countries/v1.0", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CountryDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Name.Should().Be("New Country");
        result.CountryCode.Should().Be("456");
        result.CountryCodes.Should().HaveCount(1);
        result.CountryCodes.First().Code.Should().Be("456");
        result.CountryCodes.First().IsPrimary.Should().BeTrue();
        result.ISO2.Should().Be("NC");
        result.ISO3.Should().Be("NEW");

        // Verify the country was actually created in the database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CountryDbContext>();
        var countryInDb = await context.Countries.FirstOrDefaultAsync(c => c.Id == result.Id);
        countryInDb.Should().NotBeNull();
        countryInDb!.Name.Should().Be("New Country");
    }

    [Fact]
    public async Task Create_InvalidCountry_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateCountryRequest
        {
            Name = "", // Invalid: empty name
            Continent = "Test Continent",
            CountryCode = "456",
            ISO2 = "NC",
            ISO3 = "NEW"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/countries/v1.0", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ExistingCountry_ReturnsOk()
    {
        // Arrange
        var country = new Country
        {
            Name = "Original Name",
            Continent = "Original Continent",
            ISO2 = "ON",
            ISO3 = "ORG",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CountryDbContext>();
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        // Add country code
        var countryCode = new CountryCode
        {
            CountryId = country.Id,
            Code = "789",
            IsPrimary = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        context.CountryCodes.Add(countryCode);
        await context.SaveChangesAsync();

        var request = new UpdateCountryRequest
        {
            Name = "Updated Name",
            Continent = "Updated Continent",
            CountryCode = "789",
            ISO2 = "ON",
            ISO3 = "ORG"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/countries/v1.0/{country.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CountryDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Continent.Should().Be("Updated Continent");
    }

    [Fact]
    public async Task Update_NonExistingCountry_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateCountryRequest
        {
            Name = "Updated Name",
            Continent = "Updated Continent",
            CountryCode = "999",
            ISO2 = "XX",
            ISO3 = "XXX"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/countries/v1.0/999", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ExistingCountry_ReturnsNoContent()
    {
        // Arrange
        var country = new Country
        {
            Name = "To Be Deleted",
            Continent = "Delete Continent",
            ISO2 = "DL",
            ISO3 = "DEL",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CountryDbContext>();
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        // Add country code
        var countryCode = new CountryCode
        {
            CountryId = country.Id,
            Code = "999",
            IsPrimary = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        context.CountryCodes.Add(countryCode);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/countries/v1.0/{country.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the country was actually deleted
        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<CountryDbContext>();
        var deletedCountry = await verifyContext.Countries.FirstOrDefaultAsync(c => c.Id == country.Id);
        deletedCountry.Should().BeNull();
    }

    [Fact]
    public async Task Delete_NonExistingCountry_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/countries/v1.0/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetContinents_ReturnsOk()
    {
        // Arrange
        var countries = new[]
        {
            new Country { Name = "USA", Continent = "North America", ISO2 = "US", ISO3 = "USA", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new Country { Name = "France", Continent = "Europe", ISO2 = "FR", ISO3 = "FRA", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow }
        };

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CountryDbContext>();
        context.Countries.AddRange(countries);
        await context.SaveChangesAsync();

        // Add country codes
        var countryCodes = new[]
        {
            new CountryCode { CountryId = countries[0].Id, Code = "1", IsPrimary = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
            new CountryCode { CountryId = countries[1].Id, Code = "33", IsPrimary = true, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow }
        };
        context.CountryCodes.AddRange(countryCodes);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/countries/v1.0/continents");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<string[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        result.Should().NotBeNull();
        result!.Should().Contain("North America");
        result.Should().Contain("Europe");
    }

    public void Dispose()
    {
        _client?.Dispose();
        _context?.Dispose();
    }
}

public class TestAuthenticationSchemeHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationSchemeHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) 
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim(ClaimTypes.NameIdentifier, "123")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}