using System.Net;
using System.Net.Http.Json;
using Maliev.CountryService.Api.Authorization;
using Maliev.CountryService.Api.Models.Common;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Tests.Fixtures;
using Maliev.CountryService.Tests.Testing;
using Xunit;

namespace Maliev.CountryService.Tests.Integration;

[Collection("TestDatabase")]
public class CountryServiceExtendedTests : IntegrationTestBase
{
    public CountryServiceExtendedTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task List_WithFiltering_ReturnsFilteredResults()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var adminClient = _factory.CreateAuthenticatedClient("admin", CountryAdminRoles,
            CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray());

        var r1 = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", new CreateCountryRequest { Iso2 = "QA", Iso3 = "QAA", Name = "Country A", Region = "Region1" });
        var r2 = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", new CreateCountryRequest { Iso2 = "QB", Iso3 = "QBB", Name = "Country B", Region = "Region2" });

        Assert.Equal(HttpStatusCode.Created, r1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, r2.StatusCode);

        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesList);

        // Act - Filter by region
        var response = await client.GetAsync("/country/v1/countries?region=Region1");
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<CountryResponse>>(JsonSerializerOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("Region1", result.Data.First().Region);
    }

    [Fact]
    public async Task List_WithSorting_ReturnsSortedResults()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var adminClient = _factory.CreateAuthenticatedClient("admin", CountryAdminRoles,
            CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray());

        await adminClient.PostAsJsonAsync("/country/v1/admin/countries", new CreateCountryRequest { Iso2 = "YA", Iso3 = "YAA", Name = "B Country" });
        await adminClient.PostAsJsonAsync("/country/v1/admin/countries", new CreateCountryRequest { Iso2 = "YB", Iso3 = "YBB", Name = "A Country" });

        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesList);

        // Act - Sort by name ASC
        var response = await client.GetAsync("/country/v1/countries?sortBy=name&sortOrder=asc");
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<CountryResponse>>(JsonSerializerOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        var list = result.Data.ToList();
        Assert.Equal("A Country", list[0].Name);
        Assert.Equal("B Country", list[1].Name);
    }

    [Fact]
    public async Task Search_WithDifferentFields_ReturnsResults()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var adminClient = _factory.CreateAuthenticatedClient("admin", CountryAdminRoles,
            CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray());

        await adminClient.PostAsJsonAsync("/country/v1/admin/countries", new CreateCountryRequest
        {
            Iso2 = "FR",
            Iso3 = "FRA",
            Name = "France",
            OfficialName = "French Republic"
        });

        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesSearch);

        // Act - Search by official name
        var response = await client.GetAsync("/country/v1/countries/search?query=Republic");
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<CountryResponse>>(JsonSerializerOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("France", result.Data.First().Name);

        // Search by ISO3
        var responseIso3 = await client.GetAsync("/country/v1/countries/search?query=FRA");
        var resultIso3 = await responseIso3.Content.ReadFromJsonAsync<PaginatedResponse<CountryResponse>>(JsonSerializerOptions);
        Assert.Single(resultIso3!.Data);
    }

    [Fact]
    public async Task Restore_SoftDeletedCountry_Success()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var adminPerms = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("admin", CountryAdminRoles, adminPerms);

        var createRes = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", new CreateCountryRequest { Iso2 = "DE", Iso3 = "DEU", Name = "Germany" });
        var country = await createRes.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);

        // Soft delete
        await adminClient.DeleteAsync($"/country/v1/admin/countries/{country!.Id}");

        // Verify it's gone from public list
        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesRead);
        var getRes1 = await client.GetAsync($"/country/v1/countries/{country.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getRes1.StatusCode);

        // Act - Restore
        var restoreRes = await adminClient.PostAsync($"/country/v1/admin/countries/{country.Id}/restore", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, restoreRes.StatusCode);
        var getRes2 = await client.GetAsync($"/country/v1/countries/{country.Id}");
        Assert.Equal(HttpStatusCode.OK, getRes2.StatusCode);
    }

    [Fact]
    public async Task Admin_RebuildCache_Returns204()
    {
        // Arrange
        var adminClient = _factory.CreateAuthenticatedClient("admin", CountryAdminRoles,
            new[] { CountryPermissions.SystemRebuildCache });

        // Act
        var response = await adminClient.PostAsync("/country/v1/admin/countries/rebuild-cache", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Admin_Export_ReturnsAllCountries()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var adminPerms = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).Concat(new[] { CountryPermissions.SystemExport }).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("admin", CountryAdminRoles, adminPerms);

        await adminClient.PostAsJsonAsync("/country/v1/admin/countries", new CreateCountryRequest { Iso2 = "ZA", Iso3 = "ZAA", Name = "A" });
        await adminClient.PostAsJsonAsync("/country/v1/admin/countries", new CreateCountryRequest { Iso2 = "ZB", Iso3 = "ZBB", Name = "B" });

        // Act
        var response = await adminClient.GetAsync("/country/v1/admin/countries/export");
        var result = await response.Content.ReadFromJsonAsync<List<CountryResponse>>(JsonSerializerOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }
}
