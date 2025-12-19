using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;
using Maliev.CountryService.Tests.Fixtures;
using Maliev.CountryService.Api.Models.Countries;

namespace Maliev.CountryService.Tests.Integration;

/// <summary>
/// Integration tests for optimistic concurrency control using ETags.
/// Tests the If-Match header validation and concurrent update scenarios.
/// </summary>
[Collection("TestDatabase")]
public class ConcurrencyTests : IntegrationTestBase
{
    public ConcurrencyTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task ConcurrentUpdate_SecondUpdateWithOldETag_Returns412()
    {
        // Arrange
        var adminClient = _factory.CreateAuthenticatedClient("user1", CountryAdminRoles);

        // Create a country
        var createRequest = new CreateCountryRequest
        {
            Iso2 = "XP",
            Iso3 = "CCX",
            Name = "Concurrency Test Country"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(created);
        var originalETag = created.ETag;
        Assert.NotNull(originalETag);

        // First update succeeds
        var firstUpdateRequest = new UpdateCountryRequest
        {
            Iso2 = "XP",
            Iso3 = "CCX",
            Name = "First Update"
        };

        var firstUpdateMessage = new HttpRequestMessage(HttpMethod.Put, $"/country/v1/admin/countries/{created.Id}")
        {
            Content = new StringContent(JsonSerializer.Serialize(firstUpdateRequest), Encoding.UTF8, "application/json")
        };
        firstUpdateMessage.Headers.Add("If-Match", originalETag);

        var firstUpdateResponse = await adminClient.SendAsync(firstUpdateMessage);
        Assert.Equal(HttpStatusCode.OK, firstUpdateResponse.StatusCode);

        var firstUpdated = await firstUpdateResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(firstUpdated);
        Assert.NotEqual(originalETag, firstUpdated.ETag); // ETag changed

        // Second update with OLD ETag should fail
        var secondUpdateRequest = new UpdateCountryRequest
        {
            Iso2 = "XP",
            Iso3 = "CCX",
            Name = "Second Update (should fail)"
        };

        var secondUpdateMessage = new HttpRequestMessage(HttpMethod.Put, $"/country/v1/admin/countries/{created.Id}")
        {
            Content = new StringContent(JsonSerializer.Serialize(secondUpdateRequest), Encoding.UTF8, "application/json")
        };
        secondUpdateMessage.Headers.Add("If-Match", originalETag); // Using OLD ETag

        // Act
        var secondUpdateResponse = await adminClient.SendAsync(secondUpdateMessage);

        // Assert
        Assert.Equal(HttpStatusCode.PreconditionFailed, secondUpdateResponse.StatusCode); // 412
    }

    [Fact]
    public async Task ConcurrentPatch_SecondPatchWithOldETag_Returns412()
    {
        // Arrange
        var adminClient = _factory.CreateAuthenticatedClient("user2", CountryAdminRoles);

        // Create a country
        var createRequest = new CreateCountryRequest
        {
            Iso2 = "XQ",
            Iso3 = "CCY",
            Name = "Concurrency Patch Test",
            Region = "Original Region"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(created);
        var originalETag = created.ETag;

        // First patch succeeds
        var firstPatchRequest = new PatchCountryRequest
        {
            Region = "First Patch Region"
        };

        var firstPatchMessage = new HttpRequestMessage(HttpMethod.Patch, $"/country/v1/admin/countries/{created.Id}")
        {
            Content = new StringContent(JsonSerializer.Serialize(firstPatchRequest), Encoding.UTF8, "application/json")
        };
        firstPatchMessage.Headers.Add("If-Match", originalETag);

        var firstPatchResponse = await adminClient.SendAsync(firstPatchMessage);
        Assert.Equal(HttpStatusCode.OK, firstPatchResponse.StatusCode);

        // Second patch with OLD ETag should fail
        var secondPatchRequest = new PatchCountryRequest
        {
            Region = "Second Patch Region (should fail)"
        };

        var secondPatchMessage = new HttpRequestMessage(HttpMethod.Patch, $"/country/v1/admin/countries/{created.Id}")
        {
            Content = new StringContent(JsonSerializer.Serialize(secondPatchRequest), Encoding.UTF8, "application/json")
        };
        secondPatchMessage.Headers.Add("If-Match", originalETag); // Using OLD ETag

        // Act
        var secondPatchResponse = await adminClient.SendAsync(secondPatchMessage);

        // Assert
        Assert.Equal(HttpStatusCode.PreconditionFailed, secondPatchResponse.StatusCode); // 412
    }

    [Fact]
    public async Task SequentialUpdates_WithCorrectETags_AllSucceed()
    {
        // Arrange
        var adminClient = _factory.CreateAuthenticatedClient("user3", CountryAdminRoles);

        // Create a country
        var createRequest = new CreateCountryRequest
        {
            Iso2 = "XR",
            Iso3 = "CCZ",
            Name = "Sequential Test Country"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(created);
        var currentETag = created.ETag;

        // Perform 3 sequential updates, each using the latest ETag
        for (int i = 1; i <= 3; i++)
        {
            var updateRequest = new UpdateCountryRequest
            {
                Iso2 = "XR",
                Iso3 = "CCZ",
                Name = $"Update {i}"
            };

            var updateMessage = new HttpRequestMessage(HttpMethod.Put, $"/country/v1/admin/countries/{created.Id}")
            {
                Content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json")
            };
            updateMessage.Headers.Add("If-Match", currentETag);

            var response = await adminClient.SendAsync(updateMessage);

            // Assert each update succeeds
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var updated = await response.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
            Assert.NotNull(updated);
            Assert.NotEqual(currentETag, updated.ETag); // ETag changes each time

            currentETag = updated.ETag; // Update for next iteration
        }
    }

    [Fact]
    public async Task ETag_ChangesOnEveryModification()
    {
        // Arrange
        var adminClient = _factory.CreateAuthenticatedClient("user4", CountryAdminRoles);

        // Create a country
        var createRequest = new CreateCountryRequest
        {
            Iso2 = "XA",
            Iso3 = "CCA",
            Name = "ETag Change Test"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(created);
        var etag1 = created.ETag;

        // Update the country
        var updateRequest = new UpdateCountryRequest
        {
            Iso2 = "XA",
            Iso3 = "CCA",
            Name = "Updated Name"
        };

        var updateMessage = new HttpRequestMessage(HttpMethod.Put, $"/country/v1/admin/countries/{created.Id}")
        {
            Content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json")
        };
        updateMessage.Headers.Add("If-Match", etag1);

        var updateResponse = await adminClient.SendAsync(updateMessage);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(updated);
        var etag2 = updated.ETag;

        // Patch the country
        var patchRequest = new PatchCountryRequest
        {
            Region = "New Region"
        };

        var patchMessage = new HttpRequestMessage(HttpMethod.Patch, $"/country/v1/admin/countries/{created.Id}")
        {
            Content = new StringContent(JsonSerializer.Serialize(patchRequest), Encoding.UTF8, "application/json")
        };
        patchMessage.Headers.Add("If-Match", etag2);

        var patchResponse = await adminClient.SendAsync(patchMessage);
        var patched = await patchResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(patched);
        var etag3 = patched.ETag;

        // Assert all ETags are different
        Assert.NotEqual(etag1, etag2);
        Assert.NotEqual(etag2, etag3);
        Assert.NotEqual(etag1, etag3);
    }

    [Fact]
    public async Task GetById_ReturnsETag()
    {
        // Arrange
        var adminClient = _factory.CreateAuthenticatedClient("user5", CountryAdminRoles);

        // Create a country
        var createRequest = new CreateCountryRequest
        {
            Iso2 = "XB",
            Iso3 = "CCB",
            Name = "ETag Get Test"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(created);
        var originalETag = created.ETag;

        // Act - Get the country by ID
        var getResponse = await _client.GetAsync($"/country/v1/countries/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.True(getResponse.Headers.Contains("ETag"));

        var retrieved = await getResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(retrieved);
        Assert.Equal(originalETag, retrieved.ETag);
    }

    [Fact]
    public async Task SimulateConcurrentUsers_OnlyOneUpdateSucceeds()
    {
        // Arrange
        var admin1 = _factory.CreateAuthenticatedClient("user6a", CountryAdminRoles);
        var admin2 = _factory.CreateAuthenticatedClient("user6b", CountryAdminRoles);

        // Create a country
        var createRequest = new CreateCountryRequest
        {
            Iso2 = "XC",
            Iso3 = "CCC",
            Name = "Concurrent Users Test"
        };
        var createResponse = await admin1.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(created);
        var sharedETag = created.ETag;

        // Both users prepare their updates with the same ETag
        var update1 = new UpdateCountryRequest
        {
            Iso2 = "XC",
            Iso3 = "CCC",
            Name = "User 1 Update"
        };

        var update2 = new UpdateCountryRequest
        {
            Iso2 = "XC",
            Iso3 = "CCC",
            Name = "User 2 Update"
        };

        var message1 = new HttpRequestMessage(HttpMethod.Put, $"/country/v1/admin/countries/{created.Id}")
        {
            Content = new StringContent(JsonSerializer.Serialize(update1), Encoding.UTF8, "application/json")
        };
        message1.Headers.Add("If-Match", sharedETag);

        var message2 = new HttpRequestMessage(HttpMethod.Put, $"/country/v1/admin/countries/{created.Id}")
        {
            Content = new StringContent(JsonSerializer.Serialize(update2), Encoding.UTF8, "application/json")
        };
        message2.Headers.Add("If-Match", sharedETag);

        // Act - Both users try to update simultaneously
        var response1Task = admin1.SendAsync(message1);
        var response2Task = admin2.SendAsync(message2);

        var responses = await Task.WhenAll(response1Task, response2Task);

        // Assert - One should succeed (200), one should fail (412)
        var statusCodes = responses.Select(r => r.StatusCode).OrderBy(s => s).ToArray();

        // We expect one OK and one PreconditionFailed
        Assert.Contains(HttpStatusCode.OK, statusCodes);
        Assert.Contains(HttpStatusCode.PreconditionFailed, statusCodes);
    }
}
