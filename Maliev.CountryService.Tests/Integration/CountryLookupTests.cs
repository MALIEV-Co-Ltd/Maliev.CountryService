using System.Net;
using System.Net.Http.Json;
using Xunit;
using Maliev.CountryService.Tests.Fixtures;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Api.Authorization;
using Maliev.CountryService.Tests.Testing;

namespace Maliev.CountryService.Tests.Integration;

[Collection("TestDatabase")]
public class CountryLookupTests : IntegrationTestBase
{
    public CountryLookupTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForNonExistentCountry()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesRead);

        // Act
        var response = await client.GetAsync($"/country/v1/countries/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByIso2_ReturnsNotFound_ForNonExistentCountry()
    {
        // Arrange
        var nonExistentIso2 = "XX";
        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesRead);

        // Act
        var response = await client.GetAsync($"/country/v1/countries/iso2/{nonExistentIso2}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByIso3_ReturnsNotFound_ForNonExistentCountry()
    {
        // Arrange
        var nonExistentIso3 = "XXX";
        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesRead);

        // Act
        var response = await client.GetAsync($"/country/v1/countries/iso3/{nonExistentIso3}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
