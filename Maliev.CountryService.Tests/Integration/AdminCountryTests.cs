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
    private static readonly Random _random = new();
    private static string GetRandomIso2()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        // Use a less common first letter to avoid seed data
        return "X" + chars[_random.Next(chars.Length)];
    }

    private static string GetRandomIso3()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return "X" + chars[_random.Next(chars.Length)] + chars[_random.Next(chars.Length)];
    }

    public AdminCountryTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Create_WithoutAuthentication_Returns401()
    {
        // Arrange
        var request = new CreateCountryRequest
        {
            Iso2 = GetRandomIso2(),
            Iso3 = GetRandomIso3(),
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
        var iso2 = "QA" + (char)_random.Next(65, 91); // 3 letters? No, ISO2 is 2 letters. 
        // Let's use a very safe range.
        iso2 = "" + (char)_random.Next(65, 91) + (char)_random.Next(65, 91);

        var request = new CreateCountryRequest
        {
            Iso2 = iso2,
            Iso3 = iso2 + "X",
            Name = "Test Country Create " + Guid.NewGuid().ToString("N").Substring(0, 8),
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
        Assert.Equal(iso2, result.Iso2);
        Assert.NotNull(result.ETag);
    }

    [Fact]
    public async Task Create_WithDuplicateIso2_Returns409()
    {
        // Arrange
        var permissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, permissions);

        string iso2;
        HttpResponseMessage res1;
        do
        {
            iso2 = "" + (char)_random.Next(65, 91) + (char)_random.Next(65, 91);
            var request1 = new CreateCountryRequest
            {
                Iso2 = iso2,
                Iso3 = iso2 + "D",
                Name = "Test Country " + Guid.NewGuid().ToString("N").Substring(0, 8)
            };
            res1 = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", request1);
        } while (res1.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(HttpStatusCode.Created, res1.StatusCode);

        // Try to create duplicate
        var request2 = new CreateCountryRequest
        {
            Iso2 = iso2,
            Iso3 = iso2 + "E",
            Name = "Duplicate Test"
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
        var iso2 = "" + (char)_random.Next(65, 91) + (char)_random.Next(65, 91);
        var createRequest = new CreateCountryRequest
        {
            Iso2 = iso2,
            Iso3 = iso2 + "U",
            Name = "Test Country Update"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);

        if (createResponse.StatusCode == HttpStatusCode.Conflict)
        {
            // Just skip or retry once
            iso2 = "V" + (char)_random.Next(65, 91);
            createRequest.Iso2 = iso2;
            createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        }

        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(created);

        // Try to update without If-Match header
        var updateRequest = new UpdateCountryRequest
        {
            Iso2 = iso2,
            Iso3 = iso2 + "U",
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
        var iso2 = "" + (char)_random.Next(65, 91) + (char)_random.Next(65, 91);
        var createRequest = new CreateCountryRequest
        {
            Iso2 = iso2,
            Iso3 = iso2 + "V",
            Name = "Test Country Update Valid"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            iso2 = "W" + (char)_random.Next(65, 91);
            createRequest.Iso2 = iso2;
            createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        }

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(created);
        Assert.NotNull(created.ETag);

        // Update with valid If-Match
        var updateRequest = new UpdateCountryRequest
        {
            Iso2 = iso2,
            Iso3 = iso2 + "V",
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
        var iso2 = "" + (char)_random.Next(65, 91) + (char)_random.Next(65, 91);
        var createRequest = new CreateCountryRequest
        {
            Iso2 = iso2,
            Iso3 = iso2 + "W",
            Name = "Test Country Wrong ETag"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            iso2 = "X" + (char)_random.Next(65, 91);
            createRequest.Iso2 = iso2;
            createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        }

        var created = await createResponse.Content.ReadFromJsonAsync<CountryResponse>(JsonSerializerOptions);
        Assert.NotNull(created);

        // Try to update with wrong If-Match
        var updateRequest = new UpdateCountryRequest
        {
            Iso2 = iso2,
            Iso3 = iso2 + "W",
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
        var iso2 = "" + (char)_random.Next(65, 91) + (char)_random.Next(65, 91);
        var createRequest = new CreateCountryRequest
        {
            Iso2 = iso2,
            Iso3 = iso2 + "P",
            Name = "Test Country Patch",
            Region = "Original Region"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            iso2 = "Y" + (char)_random.Next(65, 91);
            createRequest.Iso2 = iso2;
            createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        }

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
        var iso2 = "" + (char)_random.Next(65, 91) + (char)_random.Next(65, 91);
        var createRequest = new CreateCountryRequest
        {
            Iso2 = iso2,
            Iso3 = iso2 + "S",
            Name = "Test Country Delete"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            iso2 = "Z" + (char)_random.Next(65, 91);
            createRequest.Iso2 = iso2;
            createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        }

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
        var iso2 = "" + (char)_random.Next(65, 91) + (char)_random.Next(65, 91);
        var createRequest = new CreateCountryRequest
        {
            Iso2 = iso2,
            Iso3 = iso2 + "H",
            Name = "Test Country Hard Delete"
        };

        // Use a client with correct permission to create it first
        var adminWithCreate = _factory.CreateClient().WithTestAuth(_factory, CountryPermissions.CountriesCreate);
        var actualCreateResponse = await adminWithCreate.PostAsJsonAsync("/country/v1/admin/countries", createRequest);

        if (actualCreateResponse.StatusCode != HttpStatusCode.Created)
        {
            iso2 = "J" + (char)_random.Next(65, 91);
            createRequest.Iso2 = iso2;
            actualCreateResponse = await adminWithCreate.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        }

        if (actualCreateResponse.StatusCode != HttpStatusCode.Created)
        {
            var content = await actualCreateResponse.Content.ReadAsStringAsync();
            throw new Exception($"Preparation failed: Expected Created but got {actualCreateResponse.StatusCode}. Content: {content}");
        }

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
        var iso2 = "" + (char)_random.Next(65, 91) + (char)_random.Next(65, 91);
        var createRequest = new CreateCountryRequest
        {
            Iso2 = iso2,
            Iso3 = iso2 + "D",
            Name = "Test Country Super Delete"
        };
        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            iso2 = "K" + (char)_random.Next(65, 91);
            createRequest.Iso2 = iso2;
            createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/countries", createRequest);
        }

        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            var content = await createResponse.Content.ReadAsStringAsync();
            throw new Exception($"Preparation failed: Expected Created but got {createResponse.StatusCode}. Content: {content}");
        }

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
            Iso2 = "ZZ",
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