using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Maliev.CountryService.Api.Models.Common;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Tests.Fixtures;
using Xunit;

namespace Maliev.CountryService.Tests.Integration;

[Collection("TestDatabase")]
public class CountryListTests : IntegrationTestBase
{
    public CountryListTests(TestWebApplicationFactory factory) : base(factory) { } 

    [Fact]
    public async Task ListCountries_ReturnsPaginatedResults_SortedByNameAscendingByDefault()
    {
        // Arrange
        var client = _client;


        // Act
        var response = await client.GetAsync("/countries/v1/countries");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponse<CountryResponse>>(content, JsonSerializerOptions)!;

        // Assert
        Assert.NotNull(paginatedResponse);
        Assert.NotEmpty(paginatedResponse.Data);
        Assert.Equal(1, paginatedResponse.Page);
        Assert.Equal(20, paginatedResponse.PageSize); // Default page size
        Assert.True(paginatedResponse.TotalCount > 0);
        // Verify ascending order by name
        var names = paginatedResponse.Data.Select(c => c.Name).ToList();
        var sortedNames = names.OrderBy(n => n).ToList();
        Assert.Equal(sortedNames, names);
    }

    [Fact]
    public async Task ListCountries_WithPageAndPageSize_ReturnsCorrectPagination()
    {
        // Arrange
        var client = _client;
        var pageSize = 5;
        var page = 2;

        // Act
        var response = await client.GetAsync($"/countries/v1/countries?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponse<CountryResponse>>(content, JsonSerializerOptions)!;

        // Assert
        Assert.NotNull(paginatedResponse);
        Assert.Equal(pageSize, paginatedResponse.Data.Count());
        Assert.Equal(page, paginatedResponse.Page);
        Assert.Equal(pageSize, paginatedResponse.PageSize);
        Assert.True(paginatedResponse.TotalCount > 0);
    }

    [Fact]
    public async Task ListCountries_SortedByPopulationDescending_ReturnsCorrectOrder()
    {
        // Arrange
        var client = _client;

        // Act
        var response = await client.GetAsync("/countries/v1/countries?sortBy=population&sortOrder=desc");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponse<CountryResponse>>(content, JsonSerializerOptions)!;

        // Assert
        Assert.NotNull(paginatedResponse);
        Assert.NotEmpty(paginatedResponse.Data);
        // Verify descending order by population
        var populations = paginatedResponse.Data.Select(c => c.Population).ToList();
        var sortedPopulations = populations.OrderByDescending(p => p).ToList();
        Assert.Equal(sortedPopulations, populations);
    }

    [Fact]
    public async Task ListCountries_FilteredByRegion_ReturnsCorrectCountries()
    {
        // Arrange
        var client = _client;
        var region = "Europe"; // Assuming some countries in Europe exist

        // Act
        var response = await client.GetAsync($"/countries/v1/countries?region={region}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponse<CountryResponse>>(content, JsonSerializerOptions)!;

        // Assert
        Assert.NotNull(paginatedResponse);
        Assert.NotEmpty(paginatedResponse.Data);
        Assert.All(paginatedResponse.Data, c => Assert.Equal(region, c.Region));
    }

    [Fact]
    public async Task ListCountries_FilteredBySubregion_ReturnsCorrectCountries()
    {
        // Arrange
        var client = _client;
        var subregion = "Western Europe"; // Assuming some countries in Western Europe exist

        // Act
        var response = await client.GetAsync($"/countries/v1/countries?subregion={subregion}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponse<CountryResponse>>(content, JsonSerializerOptions)!;

        // Assert
        Assert.NotNull(paginatedResponse);
        Assert.NotEmpty(paginatedResponse.Data);
        Assert.All(paginatedResponse.Data, c => Assert.Equal(subregion, c.Subregion));
    }

    [Fact]
    public async Task ListCountries_SearchByName_ReturnsMatchingCountries()
    {
        // Arrange
        var client = _client;
        var searchTerm = "United"; // e.g., United States, United Kingdom

        // Act
        var response = await client.GetAsync($"/countries/v1/countries/search?query={searchTerm}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponse<CountryResponse>>(content, JsonSerializerOptions)!;

        // Assert
        Assert.NotNull(paginatedResponse);
        Assert.NotEmpty(paginatedResponse.Data);
        Assert.All(paginatedResponse.Data, c =>
            Assert.True(c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                       (c.OfficialName ?? string.Empty).Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task SearchCountries_WithShortQuery_ReturnsEmptyList()
    {
        // Arrange
        var client = _client;
        var shortQuery = "a";

        // Act
        var response = await client.GetAsync($"/countries/v1/countries/search?query={shortQuery}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponse<CountryResponse>>(content, JsonSerializerOptions)!;

        // Assert
        Assert.NotNull(paginatedResponse);
        Assert.Empty(paginatedResponse.Data);
        Assert.Equal(0, paginatedResponse.TotalCount);
    }

    [Fact]
    public async Task SearchCountries_WithNoMatch_ReturnsEmptyList()
    {
        // Arrange
        var client = _client;
        var noMatchQuery = "NonExistentCountryName";

        // Act
        var response = await client.GetAsync($"/countries/v1/countries/search?query={noMatchQuery}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponse<CountryResponse>>(content, JsonSerializerOptions)!;

        // Assert
        Assert.NotNull(paginatedResponse);
        Assert.Empty(paginatedResponse.Data);
        Assert.Equal(0, paginatedResponse.TotalCount);
    }

    [Fact]
    public async Task ListCountries_IncludesInactive_ReturnsActiveAndInactiveCountries()
    {
        // Arrange
        var client = _client;
        var inactiveCountryIso2 = "ZZ"; // Assuming this is an inactive country

        // First, create an inactive country for testing
        var adminClient = CreateAdminClient("testuser", "CountryAdmin"); // Admin client with country_admin role
        var createRequest = new CreateCountryRequest
        {
            Iso2 = inactiveCountryIso2,
            Iso3 = "ZZZ",
            Name = "Zzzyria",
            OfficialName = "Republic of Zzzyria",
            Capital = "Zzzcity",
            Region = "Asia",
            Subregion = "Western Asia",
            Population = 1000,
            AreaKm2 = 100,
            Independent = true,
            UnMember = false,
            Landlocked = false,
            Timezones = "[\"UTC+00:00\"]",
            Borders = "[\"YYY\"]",
            CallingCodes = "[\"'+00'\"]",
            TopLevelDomains = "[\".zz\"]",
            Currencies = "{\"ZZZ\":{\"name\":\"Zzzyrian Dollar\",\"symbol\":\"Z$\"}} ",
            Languages = "{\"zzz\":\"Zzzyrian\"}",
            Translations = "{\"ara\":{\"official\":\"جمهورية الزيزيرية\",\"common\":\"زيزيريا\"}}",
            Flags = "{\"png\":\"http://example.com/zz.png\",\"svg\":\"http://example.com/zz.svg\"}",
            CoatOfArms = "{\"png\":\"http://example.com/zza.png\",\"svg\":\"http://example.com/zza.svg\"}",
        };

        var createResponse = await adminClient.PostAsJsonAsync("/countries/v1/admin/countries", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdCountry = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);

        // Soft delete the country to make it inactive
        var softDeleteResponse = await adminClient.DeleteAsync($"/countries/v1/admin/countries/{createdCountry!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, softDeleteResponse.StatusCode);

        // Give cache and database more time
        await Task.Delay(1000);

        // Verify the country was soft-deleted by trying to get it directly (should still return it but as inactive)
        var checkResponse = await client.GetAsync($"/countries/v1/countries/{createdCountry.Id}");

        // Act - Request including inactive countries (with large page size to ensure we get all countries)
        // Use lowercase parameter names to match model binding conventions
        var response = await client.GetAsync("/countries/v1/countries?includeInactive=true&pageSize=100");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponse<CountryResponse>>(content, JsonSerializerOptions)!;

        // Assert
        Assert.NotNull(paginatedResponse);
        Assert.NotEmpty(paginatedResponse.Data);

        // Check if the inactive country is in the list
        var foundInactive = paginatedResponse.Data.Any(c => c.Iso2 == inactiveCountryIso2 && c.IsActive == false);

        if (!foundInactive)
        {
            // Log details for debugging
            _logger.LogWarning("Inactive country {Iso2} not found. Total countries: {Count}, Check response: {Status}",
                inactiveCountryIso2, paginatedResponse.TotalCount, checkResponse.StatusCode);

            // Still assert - but with better error message
            Assert.True(foundInactive,
                $"Expected to find inactive country {inactiveCountryIso2} in list of {paginatedResponse.TotalCount} countries. " +
                $"Check response status: {checkResponse.StatusCode}");
        }
    }
}
