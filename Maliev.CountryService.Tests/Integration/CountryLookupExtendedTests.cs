using System.Net;
using System.Net.Http.Json;
using Xunit;
using Maliev.CountryService.Tests.Fixtures;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Api.Authorization;
using Maliev.CountryService.Tests.Testing;

namespace Maliev.CountryService.Tests.Integration;

[Collection("TestDatabase")]
public class CountryLookupExtendedTests : IntegrationTestBase
{
    public CountryLookupExtendedTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForNonExistentCountry()
    {
        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesRead);
        var response = await client.GetAsync($"/country/v1/countries/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByIso2_ReturnsNotFound_ForNonExistentCountry()
    {
        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesRead);
        var response = await client.GetAsync("/country/v1/countries/iso2/XX");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByIso3_ReturnsNotFound_ForNonExistentCountry()
    {
        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesRead);
        var response = await client.GetAsync("/country/v1/countries/iso3/XXX");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsOk()
    {
        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesList);
        var response = await client.GetAsync("/country/v1/countries");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_ReturnsOk()
    {
        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesSearch);
        var response = await client.GetAsync("/country/v1/countries/search?query=test");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
