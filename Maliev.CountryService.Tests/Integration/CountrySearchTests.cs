using System.Net;
using System.Net.Http.Json;
using Xunit;
using Maliev.CountryService.Tests.Fixtures;
using Maliev.CountryService.Api.Models.Common;
using Maliev.CountryService.Api.Models.Countries;

namespace Maliev.CountryService.Tests.Integration;

/// <summary>
/// Integration tests for country search and filtering functionality.
/// Tests pagination, sorting, filtering by region, and search capabilities.
/// </summary>
[Collection("TestDatabase")]
public class CountrySearchTests : IntegrationTestBase
{
    public CountrySearchTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetCountries_WithPagination_ReturnsPagedResults()
    {
        // Act
        var response = await _client.GetAsync("/country/v1/countries?page=1&pageSize=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<CountryResponse>>(JsonSerializerOptions);
        Assert.NotNull(result);
        Assert.True(result.Data.Count() <= 2);
        Assert.True(result.TotalCount >= 0);
    }

    [Fact]
    public async Task GetCountries_FilterByRegion_ReturnsFilteredResults()
    {
        // Act - Filter by Americas region
        var response = await _client.GetAsync("/country/v1/countries?region=Americas");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<CountryResponse>>(JsonSerializerOptions);
        Assert.NotNull(result);

        // All returned countries should be in Americas region
        foreach (var country in result.Data)
        {
            Assert.Equal("Americas", country.Region);
        }
    }

    [Fact]
    public async Task GetCountries_SortByName_ReturnsSortedResults()
    {
        // Act
        var response = await _client.GetAsync("/country/v1/countries?sortBy=name&sortOrder=asc&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<CountryResponse>>(JsonSerializerOptions);
        Assert.NotNull(result);

        // Verify sorting
        var names = result.Data.Select(c => c.Name).ToList();
        var sortedNames = names.OrderBy(n => n).ToList();
        Assert.Equal(sortedNames, names);
    }

    [Fact]
    public async Task GetCountries_InvalidPage_Returns400()
    {
        // Act
        var response = await _client.GetAsync("/country/v1/countries?page=0&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCountries_ExcessivePageSize_Returns400()
    {
        // Act
        var response = await _client.GetAsync("/country/v1/countries?page=1&pageSize=1001");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchCountries_ByName_ReturnsMatchingResults()
    {
        // Act
        var response = await _client.GetAsync("/country/v1/countries/search?query=united");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<CountryResponse>>(JsonSerializerOptions);
        Assert.NotNull(result);

        // All results should contain "united" in the name or official name (case-insensitive)
        foreach (var country in result.Data)
        {
            bool containsInName = country.Name.ToLower().Contains("united");
            bool containsInOfficialName = (country.OfficialName ?? string.Empty).ToLower().Contains("united");
            Assert.True(containsInName || containsInOfficialName,
                $"Country '{country.Name}' (Official: {country.OfficialName}) should contain 'united' in name or official name");
        }
    }

    [Fact]
    public async Task SearchCountries_EmptyQuery_Returns400()
    {
        // Act
        var response = await _client.GetAsync("/country/v1/countries/search?query=");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchCountries_WithPagination_ReturnsPagedResults()
    {
        // Act
        var response = await _client.GetAsync("/country/v1/countries/search?query=a&page=1&pageSize=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<CountryResponse>>(JsonSerializerOptions);
        Assert.NotNull(result);
        Assert.True(result.Data.Count() <= 5);
    }

    [Fact]
    public async Task GetCountries_MultipleFilters_AppliesAllFilters()
    {
        // Act - Combine region filter with sorting
        var response = await _client.GetAsync("/country/v1/countries?region=Europe&sortBy=name&sortOrder=desc");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<CountryResponse>>(JsonSerializerOptions);
        Assert.NotNull(result);

        // All should be in Europe
        foreach (var country in result.Data)
        {
            Assert.Equal("Europe", country.Region);
        }

        // Verify descending sort
        var names = result.Data.Select(c => c.Name).ToList();
        var sortedNames = names.OrderByDescending(n => n).ToList();
        Assert.Equal(sortedNames, names);
    }
}
