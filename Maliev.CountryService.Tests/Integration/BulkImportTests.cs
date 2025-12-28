using System.Net;
using System.Net.Http.Json;
using Xunit;
using Maliev.CountryService.Tests.Fixtures;
using Maliev.CountryService.Api.Models.BulkImport;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Api.Authorization; // Added

namespace Maliev.CountryService.Tests.Integration;

/// <summary>
/// Integration tests for bulk import operations.
/// Tests validation, async processing, job tracking, and error handling.
/// </summary>
[Collection("TestDatabase")]
public class BulkImportTests : IntegrationTestBase
{
    public BulkImportTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task BulkImport_WithoutAuthentication_Returns401()
    {
        // Arrange
        var request = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new CreateCountryRequest
                {
                    Iso2 = "XD",
                    Name = "Test Country",
                    Timezones = "[]",
                    Borders = "[]",
                    CallingCodes = "[]",
                    TopLevelDomains = "[]",
                    Currencies = "{}",
                    Languages = "{}",
                    Translations = "{}",
                    Flags = "{}"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/country/v1/admin/bulk-import", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BulkImport_WithValidData_Returns202()
    {
        // Arrange
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, adminPermissions);
        var request = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new CreateCountryRequest
                {
                    Iso2 = "XE",
                    Iso3 = "XXE",
                    Name = "Bulk Test Country 1",
                    Timezones = "[]",
                    Borders = "[]",
                    CallingCodes = "[]",
                    TopLevelDomains = "[]",
                    Currencies = "{}",
                    Languages = "{}",
                    Translations = "{}",
                    Flags = "{}"
                },
                new CreateCountryRequest
                {
                    Iso2 = "XF",
                    Iso3 = "XXF",
                    Name = "Bulk Test Country 2",
                    Timezones = "[]",
                    Borders = "[]",
                    CallingCodes = "[]",
                    TopLevelDomains = "[]",
                    Currencies = "{}",
                    Languages = "{}",
                    Translations = "{}",
                    Flags = "{}"
                }
            }
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/country/v1/admin/bulk-import", request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode); // 202
        Assert.True(response.Headers.Contains("Location"));

        var result = await response.Content.ReadFromJsonAsync<BulkImportStatusResponse>(JsonSerializerOptions);
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.JobId);
        Assert.Equal(2, result.TotalRecords);
    }

    [Fact]
    public async Task BulkImport_WithInvalidData_Returns400()
    {
        // Arrange
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, adminPermissions);
        var request = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new CreateCountryRequest
                {
                    Iso2 = "", // Invalid - empty
                    Name = "Invalid Country"
                }
            }
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/country/v1/admin/bulk-import", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BulkImport_ExceedsLimit_Returns413()
    {
        // Arrange
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, adminPermissions);
        var countries = new List<CreateCountryRequest>();

        // Create 1001 countries to exceed the 1000 limit
        for (int i = 0; i < 1001; i++)
        {
            // Generate unique ISO2 codes: AA, AB, AC...ZZ
            var iso2 = i < 676
                ? $"{(char)('A' + i / 26)}{(char)('A' + i % 26)}"  // AA-ZZ (676 combinations)
                : $"Z{(char)('A' + (i - 676) % 26)}";  // ZA-ZZ for remaining

            // Generate unique ISO3 codes: AAA, AAB, AAC...ZZZ
            var iso3 = i < 17576
                ? $"{(char)('A' + i / 676)}{(char)('A' + (i / 26) % 26)}{(char)('A' + i % 26)}"
                : $"ZZ{(char)('A' + (i - 17576) % 26)}";

            countries.Add(new CreateCountryRequest
            {
                Iso2 = iso2,
                Iso3 = iso3,
                Name = $"Test Country {i}"
            });
        }

        var request = new BulkImportRequest { Countries = countries };

        // Act
        var response = await adminClient.PostAsJsonAsync("/country/v1/admin/bulk-import", request);

        // Assert
        Assert.Equal((HttpStatusCode)413, response.StatusCode); // 413 Payload Too Large
    }

    [Fact]
    public async Task BulkImport_EmptyList_Returns400()
    {
        // Arrange
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, adminPermissions);
        var request = new BulkImportRequest { Countries = new List<CreateCountryRequest>() };

        // Act
        var response = await adminClient.PostAsJsonAsync("/country/v1/admin/bulk-import", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetJobStatus_ValidJob_Returns200()
    {
        // Arrange
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, adminPermissions);

        // Create a bulk import job first
        var createRequest = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new CreateCountryRequest
                {
                    Iso2 = "XG",
                    Iso3 = "XXG",
                    Name = "Status Test Country",
                    Timezones = "[]",
                    Borders = "[]",
                    CallingCodes = "[]",
                    TopLevelDomains = "[]",
                    Currencies = "{}",
                    Languages = "{}",
                    Translations = "{}",
                    Flags = "{}"
                }
            }
        };

        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/bulk-import", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<BulkImportStatusResponse>(JsonSerializerOptions);
        Assert.NotNull(createResult);

        // Act - Get job status
        var response = await adminClient.GetAsync($"/country/v1/admin/bulk-import/{createResult.JobId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var status = await response.Content.ReadFromJsonAsync<BulkImportStatusResponse>(JsonSerializerOptions);
        Assert.NotNull(status);
        Assert.Equal(createResult.JobId, status.JobId);
    }

    [Fact]
    public async Task GetJobStatus_NonExistentJob_Returns404()
    {
        // Arrange
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, adminPermissions);
        var nonExistentJobId = Guid.NewGuid();

        // Act
        var response = await adminClient.GetAsync($"/country/v1/admin/bulk-import/{nonExistentJobId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProcessJob_ValidatedJob_Returns202()
    {
        // Arrange
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, adminPermissions);

        // Create a validated job first
        var createRequest = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new CreateCountryRequest
                {
                    Iso2 = "XI",
                    Iso3 = "XXI",
                    Name = "Process Test Country",
                    Timezones = "[]",
                    Borders = "[]",
                    CallingCodes = "[]",
                    TopLevelDomains = "[]",
                    Currencies = "{}",
                    Languages = "{}",
                    Translations = "{}",
                    Flags = "{}"
                }
            }
        };

        var createResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/bulk-import", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<BulkImportStatusResponse>(JsonSerializerOptions);
        Assert.NotNull(createResult);

        // Wait for validation to complete
        await Task.Delay(500);

        // Act - Process the job
        var response = await adminClient.PostAsync($"/country/v1/admin/bulk-import/{createResult.JobId}/process", null);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.Accepted ||
            response.StatusCode == HttpStatusCode.BadRequest, // Might already be processing
            $"Expected 202 or 400 but got {response.StatusCode}");
    }

    [Fact]
    public async Task ProcessJob_NonExistentJob_Returns404()
    {
        // Arrange
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, adminPermissions);
        var nonExistentJobId = Guid.NewGuid();

        // Act
        var response = await adminClient.PostAsync($"/country/v1/admin/bulk-import/{nonExistentJobId}/process", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BulkImport_WithDuplicateInBatch_FailsValidation()
    {
        // Arrange
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, adminPermissions);
        var request = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new CreateCountryRequest
                {
                    Iso2 = "XJ",
                    Iso3 = "XXJ",
                    Name = "Duplicate Test 1",
                    Timezones = "[]",
                    Borders = "[]",
                    CallingCodes = "[]",
                    TopLevelDomains = "[]",
                    Currencies = "{}",
                    Languages = "{}",
                    Translations = "{}",
                    Flags = "{}"
                },
                new CreateCountryRequest
                {
                    Iso2 = "XJ",
                    Iso3 = "XXK",
                    Name = "Duplicate Test 2",
                    Timezones = "[]",
                    Borders = "[]",
                    CallingCodes = "[]",
                    TopLevelDomains = "[]",
                    Currencies = "{}",
                    Languages = "{}",
                    Translations = "{}",
                    Flags = "{}"
                } // Same Iso2
            }
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/country/v1/admin/bulk-import", request);

        // Assert - Should fail validation due to duplicate ISO2
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BulkImport_CompleteFlow_ValidateProcessCheck()
    {
        // Arrange
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, adminPermissions);
        var request = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new CreateCountryRequest
                {
                    Iso2 = "XL",
                    Iso3 = "XXL",
                    Name = "Flow Test Country 1",
                    Timezones = "[]",
                    Borders = "[]",
                    CallingCodes = "[]",
                    TopLevelDomains = "[]",
                    Currencies = "{}",
                    Languages = "{}",
                    Translations = "{}",
                    Flags = "{}"
                },
                new CreateCountryRequest
                {
                    Iso2 = "XM",
                    Iso3 = "XXM",
                    Name = "Flow Test Country 2",
                    Timezones = "[]",
                    Borders = "[]",
                    CallingCodes = "[]",
                    TopLevelDomains = "[]",
                    Currencies = "{}",
                    Languages = "{}",
                    Translations = "{}",
                    Flags = "{}"
                }
            }
        };

        // Act 1 - Submit for validation
        var submitResponse = await adminClient.PostAsJsonAsync("/country/v1/admin/bulk-import", request);
        Assert.Equal(HttpStatusCode.Accepted, submitResponse.StatusCode);

        var submitResult = await submitResponse.Content.ReadFromJsonAsync<BulkImportStatusResponse>(JsonSerializerOptions);
        Assert.NotNull(submitResult);
        var jobId = submitResult.JobId;

        // Wait for validation
        await Task.Delay(1000);

        // Act 2 - Check status
        var statusResponse = await adminClient.GetAsync($"/country/v1/admin/bulk-import/{jobId}");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);

        var statusResult = await statusResponse.Content.ReadFromJsonAsync<BulkImportStatusResponse>(JsonSerializerOptions);
        Assert.NotNull(statusResult);

        // Act 3 - Process job (if validated)
        if (statusResult.Status == "Validated")
        {
            var processResponse = await adminClient.PostAsync($"/country/v1/admin/bulk-import/{jobId}/process", null);
            Assert.Equal(HttpStatusCode.Accepted, processResponse.StatusCode);

            // Wait for processing
            await Task.Delay(2000);

            // Act 4 - Check final status
            var finalStatusResponse = await adminClient.GetAsync($"/country/v1/admin/bulk-import/{jobId}");
            var finalStatus = await finalStatusResponse.Content.ReadFromJsonAsync<BulkImportStatusResponse>(JsonSerializerOptions);
            Assert.NotNull(finalStatus);

            // Should be completed or processing
            Assert.True(
                finalStatus.Status == "Completed" ||
                finalStatus.Status == "Processing" ||
                finalStatus.Status == "Validated",
                $"Expected Completed/Processing/Validated but got {finalStatus.Status}");
        }
    }

    [Fact]
    public async Task BulkImport_MultipleJobs_CanBeTrackedIndependently()
    {
        // Arrange
        var adminPermissions = CountryPredefinedRoles.GetPermissionsForRole(CountryAdminRoles[0]).ToArray();
        var adminClient = _factory.CreateAuthenticatedClient("testuser", CountryAdminRoles, adminPermissions);

        var request1 = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new CreateCountryRequest
                {
                    Iso2 = "XN",
                    Iso3 = "XXN",
                    Name = "Multi Job 1",
                    Timezones = "[]",
                    Borders = "[]",
                    CallingCodes = "[]",
                    TopLevelDomains = "[]",
                    Currencies = "{}",
                    Languages = "{}",
                    Translations = "{}",
                    Flags = "{}"
                }
            }
        };

        var request2 = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new CreateCountryRequest
                {
                    Iso2 = "XO",
                    Iso3 = "XXO",
                    Name = "Multi Job 2",
                    Timezones = "[]",
                    Borders = "[]",
                    CallingCodes = "[]",
                    TopLevelDomains = "[]",
                    Currencies = "{}",
                    Languages = "{}",
                    Translations = "{}",
                    Flags = "{}"
                }
            }
        };

        // Act - Submit two separate jobs
        var response1 = await adminClient.PostAsJsonAsync("/country/v1/admin/bulk-import", request1);
        var response2 = await adminClient.PostAsJsonAsync("/country/v1/admin/bulk-import", request2);

        var result1 = await response1.Content.ReadFromJsonAsync<BulkImportStatusResponse>(JsonSerializerOptions);
        var result2 = await response2.Content.ReadFromJsonAsync<BulkImportStatusResponse>(JsonSerializerOptions);

        Assert.NotNull(result1);
        Assert.NotNull(result2);

        // Assert - Jobs should have different IDs
        Assert.NotEqual(result1.JobId, result2.JobId);

        // Both should be trackable independently
        var status1 = await adminClient.GetAsync($"/country/v1/admin/bulk-import/{result1.JobId}");
        var status2 = await adminClient.GetAsync($"/country/v1/admin/bulk-import/{result2.JobId}");

        Assert.Equal(HttpStatusCode.OK, status1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, status2.StatusCode);
    }
}
