using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;
using Maliev.CountryService.Tests.Fixtures;
using Maliev.CountryService.Tests.Testing;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Api.Authorization;

namespace Maliev.CountryService.Tests.Integration;

/// <summary>
/// Integration tests for Admin Country CRUD operations.
/// Tests authentication, authorization, optimistic concurrency, and audit logging.
/// </summary>
[Collection("TestDatabase")]
public class AdminCountryTests : IntegrationTestBase
{
    public AdminCountryTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Create_WithoutAuthentication_Returns401()
    {
        // Arrange
        var request = new CreateCountryRequest
        {
            Iso2 = "XX",
            Iso3 = "XXX",
            Name = "Test Country"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/country/v1/admin/countries", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithAuthentication_Returns201()
    {
        // Arrange
        var permissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, permissions);
        var request = new CreateCountryRequest
        {
            Iso2 = "TA",
            Iso3 = "TSA",
            Name = "Test Country Create",
            Region = "Test Region"
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", request);

        // Assert
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected Created but got {response.StatusCode}. Content: {content}");
        }
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(response.Headers.Contains("Location"));
        Assert.True(response.Headers.Contains("ETag"));

        var result = await response.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(result);
        Assert.Equal("TA", result.Iso2);
        Assert.Equal("TSA", result.Iso3);
        Assert.Equal("Test Country Create", result.Name);
        Assert.NotNull(result.ETag);
    }

    [Fact]
    public async Task Create_WithDuplicateIso2_Returns409()
    {
        // Arrange
        var permissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, permissions);
        var request1 = new CreateCountryRequest
        {
            Iso2 = "TB",
            Iso3 = "TSB",
            Name = "Test Country 1"
        };

        // Create first country
        await adminClient.PostAsJsonAsync("/country/v1/admin/countries", request1);

        // Try to create duplicate
        var request2 = new CreateCountryRequest
        {
            Iso2 = "TB",
            Iso3 = "TSX",
            Name = "Test Country 2"
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", request2);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithoutIfMatch_Returns428()
    {
        // Arrange
        var permissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, permissions);

        // Create a country first
        var createRequest = new CreateCountryRequest
        {
            Iso2 = "TC",
            Iso3 = "TSC",
            Name = "Test Country Update"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(created);

        // Try to update without If-Match header
        var updateRequest = new UpdateCountryRequest
        {
            Iso2 = "TC",
            Iso3 = "TSC",
            Name = "Updated Name"
        };

        // Act
        var response = await adminClient.PutAsJsonAsync($"/country/v1/admin/countries/{created.Id}", updateRequest);

        // Assert
        Assert.Equal((HttpStatusCode)428, response.StatusCode); // 428 Precondition Required
    }

    [Fact]
    public async Task Update_WithValidIfMatch_Returns200()
    {
        // Arrange
        var permissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, permissions);

        // Create a country first
        var createRequest = new CreateCountryRequest
        {
            Iso2 = "XD",
            Iso3 = "XDD",
            Name = "Test Country Update Valid"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(created);
        Assert.NotNull(created.ETag);

        // Update with valid If-Match
        var updateRequest = new UpdateCountryRequest
        {
            Iso2 = "XD",
            Iso3 = "XDD",
            Name = "Updated Name Valid"
        };

        var updateMessage = new HttpRequestMessage(HttpMethod.Put, $"/country/v1/admin/countries/{created.Id}")
        {
            Content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json")
        };
        updateMessage.Headers.Add("If-Match", created.ETag);

        // Act
        var response = await adminClient.SendAsync(updateMessage);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("ETag"));

        var result = await response.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(result);
        Assert.Equal("Updated Name Valid", result.Name);
        Assert.NotEqual(created.ETag, result.ETag); // ETag should change after update
    }

    [Fact]
    public async Task Update_WithWrongIfMatch_Returns412()
    {
        // Arrange
        var permissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, permissions);

        // Create a country first
        var createRequest = new CreateCountryRequest
        {
            Iso2 = "TE",
            Iso3 = "TSE",
            Name = "Test Country Wrong ETag"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(created);

        // Try to update with wrong If-Match
        var updateRequest = new UpdateCountryRequest
        {
            Iso2 = "TE",
            Iso3 = "TSE",
            Name = "Updated Name Wrong"
        };

        var updateMessage = new HttpRequestMessage(HttpMethod.Put, $"/country/v1/admin/countries/{created.Id}")
        {
            Content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json")
        };
        updateMessage.Headers.Add("If-Match", "\"wrong-etag-value\"");

        // Act
        var response = await adminClient.SendAsync(updateMessage);

        // Assert
        Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode); // 412
    }

    [Fact]
    public async Task Patch_WithValidIfMatch_Returns200()
    {
        // Arrange
        var permissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, permissions);

        // Create a country first
        var createRequest = new CreateCountryRequest
        {
            Iso2 = "TF",
            Iso3 = "TSF",
            Name = "Test Country Patch",
            Region = "Original Region"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(created);

        // Patch only the region
        var patchRequest = new PatchCountryRequest
        {
            Region = "Updated Region"
        };

        var patchMessage = new HttpRequestMessage(HttpMethod.Patch, $"/country/v1/admin/countries/{created.Id}")
        {
            Content = new StringContent(JsonSerializer.Serialize(patchRequest), Encoding.UTF8, "application/json")
        };
        patchMessage.Headers.Add("If-Match", created.ETag);

        // Act
        var response = await adminClient.SendAsync(patchMessage);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(result);
        Assert.Equal("Test Country Patch", result.Name); // Name unchanged
        Assert.Equal("Updated Region", result.Region); // Region updated
    }

    [Fact]
    public async Task SoftDelete_WithAuthentication_Returns204()
    {
        // Arrange
        var permissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, permissions);

        // Create a country first
        var createRequest = new CreateCountryRequest
        {
            Iso2 = "XE",
            Iso3 = "XEE",
            Name = "Test Country Delete"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(created);

        // Act - Soft delete
        var response = await adminClient.DeleteAsync($"/country/v1/admin/countries/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the country is no longer accessible (soft-deleted)
        var getResponse = await _client.GetAsync($"/country/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task HardDelete_WithoutSuperAdminRole_Returns403()
    {
        // Arrange
        var adminClient = _factory.CreateClient().WithTestAuth(_factory, "Permission:country.not-hard-delete");

        // Create a country first
        var createRequest = new CreateCountryRequest
        {
            Iso2 = "XH",
            Iso3 = "TSH",
            Name = "Test Country Hard Delete"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);

        // Use a client with correct permission to create it first if necessary, 
        // but here we just need ANY country ID to try deleting it.
        // Assuming the ID 202 from previous failed run might not be there, 
        // we should really get a valid ID.

        var adminWithCreate = _factory.CreateClient().WithTestAuth(_factory, CountryPermissions.CountriesCreate);
        var actualCreateResponse = await adminWithCreate.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        var created = await actualCreateResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(created);

        // Act - Try hard delete without super_admin permission
        var response = await adminClient.DeleteAsync($"/country/v1/admin/countries/{created.Id}/hard-delete");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task HardDelete_WithSuperAdminRole_Returns204()
    {
        // Arrange - Try with both possible role names
        var superAdminPerms = CountryPredefinedRoles.GetPermissionsForRole(SuperAdminRoles[0]).ToArray();
        var superAdminClient = _factory.CreateAuthenticatedClient("superadmin", SuperAdminRoles, superAdminPerms);

        // Create a country first using country_admin client
        var adminPerms = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, adminPerms);
        var createRequest = new CreateCountryRequest
        {
            Iso2 = "TI",
            Iso3 = "TSI",
            Name = "Test Country Super Delete"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(created);

        // Act - Hard delete with super_admin role
        var response = await superAdminClient.DeleteAsync($"/country/v1/admin/countries/{created.Id}/hard-delete");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the country is completely gone
        var getResponse = await _client.GetAsync($"/country/v1/country/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidData_Returns400()
    {
        // Arrange
        var permissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, permissions);
        var request = new CreateCountryRequest
        {
            Iso2 = "", // Invalid - empty
            Name = "Test Country"
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_NonExistentCountry_Returns404()
    {
        // Arrange
        var permissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, permissions);
        var updateRequest = new UpdateCountryRequest
        {
            Iso2 = "XX",
            Name = "Non Existent"
        };

        var updateMessage = new HttpRequestMessage(HttpMethod.Put, "/country/v1/admin/countries/999999")
        {
            Content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json")
        };
        updateMessage.Headers.Add("If-Match", "\"some-etag\"");

        // Act
        var response = await adminClient.SendAsync(updateMessage);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
