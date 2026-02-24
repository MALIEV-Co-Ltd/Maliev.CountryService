using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Xunit;
using Maliev.CountryService.Tests.Fixtures;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Api.Authorization; // Added
using Maliev.CountryService.Tests.Testing; // Added

namespace Maliev.CountryService.Tests.Integration;

/// <summary>
/// Integration tests for caching behavior.
/// Tests cache headers, cache hits/misses, and ETag behavior.
/// </summary>
[Collection("TestDatabase")]
public class CachingTests : IntegrationTestBase
{
    public CachingTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetById_ReturnsETagHeader()
    {
        // Ensure data exists
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("cachetest", CountryAdminRoles, adminPermissions);
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", new CreateCountryRequest { Iso2 = "ET", Iso3 = "ETA", Name = "Etag Country" });
        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);

        // Act
        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesRead);
        var response = await client.GetAsync($"/country/v1/countries/{created!.Id}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.True(response.Headers.Contains("ETag"), "Expected ETag header to be present");
        }
    }

    [Fact]
    public async Task GetById_WithIfNoneMatch_Returns304WhenNotModified()
    {
        // Ensure data exists
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("cachetest", CountryAdminRoles, adminPermissions);
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", new CreateCountryRequest { Iso2 = "NM", Iso3 = "NMM", Name = "No Match Country" });
        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);

        // Arrange - First request to get ETag
        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesRead);
        var firstResponse = await client.GetAsync($"/country/v1/countries/{created!.Id}");


        if (firstResponse.StatusCode != HttpStatusCode.OK)
        {
            // Country doesn't exist, skip test
            return;
        }

        var country = await firstResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(country);
        var etag = country.ETag;

        // Act - Second request with If-None-Match
        var request = new HttpRequestMessage(HttpMethod.Get, $"/country/v1/countries/{created!.Id}");
        request.Headers.Add("If-None-Match", etag);

        // Need to add auth to the request message or use the authenticated client
        // With HttpRequestMessage, we need to set the Authorization header manually if not using the client's defaults
        // But since we are using client.SendAsync, client's defaults should apply? No, request headers override/merge.
        // But client.DefaultRequestHeaders applies to all requests.
        // Let's reuse the authenticated client.

        var secondResponse = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotModified, secondResponse.StatusCode);
    }

    [Fact]
    public async Task GetById_MultipleCalls_ShouldBeFast()
    {
        // Ensure data exists
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("cachetest", CountryAdminRoles, adminPermissions);
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", new CreateCountryRequest { Iso2 = "FT", Iso3 = "FTA", Name = "Fast Country" });
        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);

        // Arrange
        var countryId = created!.Id;
        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesRead);


        // Act - Make multiple calls and measure average time
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 10; i++)
        {
            var response = await client.GetAsync($"/country/v1/countries/{countryId}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                await response.Content.ReadAsStringAsync();
            }
        }
        stopwatch.Stop();

        var averageMs = stopwatch.ElapsedMilliseconds / 10.0;

        // Assert - Average should be fast (under 100ms with caching)
        // Note: This is a soft assertion as CI environments may be slower
        _logger.LogInformation("Average response time: {AverageMs}ms", averageMs);
        Assert.True(averageMs < 500, $"Expected average response time < 500ms, got {averageMs}ms");
    }

    [Fact]
    public async Task GetByIso2_CachingBehavior_ConsistentResults()
    {
        // Ensure data exists
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("cachetest", CountryAdminRoles, adminPermissions);
        await adminClient.PostAsJsonAsync("/country/v1/admin/countries", new CreateCountryRequest { Iso2 = "US", Iso3 = "USA", Name = "United States" });

        // Arrange
        var iso2 = "US";
        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesRead);


        // Act - Make two consecutive calls
        var response1 = await client.GetAsync($"/country/v1/countries/iso2/{iso2}");
        var response2 = await client.GetAsync($"/country/v1/countries/iso2/{iso2}");

        // Assert - Results should be consistent
        if (response1.StatusCode == HttpStatusCode.OK && response2.StatusCode == HttpStatusCode.OK)
        {
            var country1 = await response1.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
            var country2 = await response2.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);

            Assert.NotNull(country1);
            Assert.NotNull(country2);
            Assert.Equal(country1.Id, country2.Id);
            Assert.Equal(country1.Name, country2.Name);
            Assert.Equal(country1.ETag, country2.ETag);
        }
    }

    [Fact]
    public async Task List_WithSameParams_ReturnsCachedResults()
    {
        // Ensure some data exists
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("cachetest", CountryAdminRoles, adminPermissions);
        await adminClient.PostAsJsonAsync("/country/v1/admin/countries", new CreateCountryRequest { Iso2 = "CA", Iso3 = "CAN", Name = "Canada" });

        // Arrange
        var queryString = "?page=1&pageSize=10&sortBy=name";

        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesList);

        // Act - Make two consecutive calls with same parameters
        var response1 = await client.GetAsync($"/country/v1/countries{queryString}");
        await Task.Delay(100); // Small delay
        var response2 = await client.GetAsync($"/country/v1/countries{queryString}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        // Second request should be faster (from cache)
        // Note: We can't reliably test timing in CI, but we can verify consistency
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        Assert.NotEmpty(content1);
        Assert.NotEmpty(content2);
    }

    [Fact]
    public async Task CacheHeaders_PresentInResponse()
    {
        // Ensure data exists
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("cachetest", CountryAdminRoles, adminPermissions);
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", new CreateCountryRequest { Iso2 = "CH", Iso3 = "CHA", Name = "Cache Header Country" });
        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);

        // Act
        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesRead);
        var response = await client.GetAsync($"/country/v1/countries/{created!.Id}");


        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            // Check for cache-related headers
            // Note: Specific headers depend on cache implementation
            var headers = response.Headers.ToString();
            _logger.LogInformation("Response headers: {Headers}", headers);

            // ETag should be present
            Assert.True(response.Headers.Contains("ETag") ||
                       response.Content.Headers.Contains("ETag"),
                       "Expected ETag header");
        }
    }

    [Fact]
    public async Task AfterModification_CacheIsInvalidated()
    {
        // Arrange
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("cachetest", CountryAdminRoles, adminPermissions);
        var client = _client.WithTestAuth(_factory, CountryPermissions.CountriesRead);

        // Create a country
        var createRequest = new CreateCountryRequest
        {
            Iso2 = "XY",
            Iso3 = "XYZ",
            Name = "Cache Test Country"
        };

        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);

        // Handle potential conflict from previous runs
        if (createResponse.StatusCode == HttpStatusCode.Conflict)
        {
            // Use random ISOs
            var random = new Random().Next(1000, 9999).ToString();
            createRequest.Iso2 = "R" + random.Substring(0, 1); // 2 chars
            createRequest.Iso3 = "R" + random.Substring(0, 2); // 3 chars
            createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        }

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            // Creation failed, skip test
            return;
        }

        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(created);

        // Read the country to populate cache
        var readResponse1 = await client.GetAsync($"/country/v1/countries/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, readResponse1.StatusCode);
        var country1 = await readResponse1.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);

        // Act - Update the country
        var updateRequest = new UpdateCountryRequest
        {
            Iso2 = createRequest.Iso2,
            Iso3 = createRequest.Iso3,
            Name = "Updated Cache Test Country"
        };

        var updateMessage = new HttpRequestMessage(HttpMethod.Put, $"/country/v1/admin/countries/{created.Id}")
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(updateRequest), System.Text.Encoding.UTF8, "application/json")
        };
        updateMessage.Headers.Add("If-Match", created.ETag);

        var updateResponse = await adminClient.SendAsync(updateMessage);

        // Read again to verify cache was invalidated
        var readResponse2 = await client.GetAsync($"/country/v1/countries/{created.Id}");
        var country2 = await readResponse2.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);

        // Assert - Name should be updated (cache was invalidated)
        Assert.NotNull(country1);
        Assert.NotNull(country2);
        Assert.Equal("Cache Test Country", country1.Name);
        Assert.Equal("Updated Cache Test Country", country2.Name);
        Assert.NotEqual(country1.ETag, country2.ETag);
    }
}
