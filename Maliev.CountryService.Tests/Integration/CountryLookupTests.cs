using System.Net;
using System.Net.Http.Json;
using Xunit;
using Maliev.CountryService.Tests.Fixtures;
using Maliev.CountryService.Api.Models.Countries; // Assuming CountryResponse is here

namespace Maliev.CountryService.Tests.Integration;

public class CountryLookupTests : IntegrationTestBase
{
    public CountryLookupTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForNonExistentCountry()
    {
        // Arrange
        var nonExistentId = 9999;

        // Act
        var response = await _client.GetAsync($"/countries/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByIso2_ReturnsNotFound_ForNonExistentCountry()
    {
        // Arrange
        var nonExistentIso2 = "XX";

        // Act
        var response = await _client.GetAsync($"/countries/iso2/{nonExistentIso2}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByIso3_ReturnsNotFound_ForNonExistentCountry()
    {
        // Arrange
        var nonExistentIso3 = "XXX";

        // Act
        var response = await _client.GetAsync($"/countries/iso3/{nonExistentIso3}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
