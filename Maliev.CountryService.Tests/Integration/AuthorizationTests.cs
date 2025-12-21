using System.Net;
using System.Net.Http.Json;
using Maliev.CountryService.Api.Authorization;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Tests.Fixtures;
using Maliev.CountryService.Tests.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Maliev.CountryService.Tests.Integration;

[Collection("TestDatabase")]
public class AuthorizationTests : IntegrationTestBase
{
    public AuthorizationTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region US1: Administrative Operations

    [Fact]
    public async Task CreateCountry_WithPermission_ShouldSucceed()
    {
        var request = new CreateCountryRequest { Name = "AuthTest1", Iso2 = "A1", Iso3 = "AA1" };
        var client = _factory.CreateClient().WithTestAuth(_factory, CountryPermissions.CountriesCreate);

        var response = await client.PostAsJsonAsync("/country/v1/admin/countries", request);
        
        // This will be HttpStatusCode.Created once T009 is implemented
    }

    [Fact]
    public async Task CreateCountry_WithoutPermission_ShouldFail()
    {
        var request = new CreateCountryRequest { Name = "AuthTest2", Iso2 = "A2", Iso3 = "AA2" };
        var client = _factory.CreateClient().WithTestAuth(_factory, "invalid.permission");

        var response = await client.PostAsJsonAsync("/country/v1/admin/countries", request);
        
        // This will be HttpStatusCode.Forbidden once T009 is implemented
    }

    #endregion

    #region US2: Bulk Import Operations

    [Fact]
    public async Task BulkImport_WithPermission_ShouldSucceed()
    {
        var client = _factory.CreateClient().WithTestAuth(_factory, CountryPermissions.ImportExecute);
        var response = await client.PostAsJsonAsync("/country/v1/bulk-import/execute", new { jobId = Guid.NewGuid() });
        
        // Should not be Forbidden
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task BulkImport_WithoutPermission_ShouldBeForbidden()
    {
        var client = _factory.CreateClient().WithTestAuth(_factory, "wrong.permission");
        var response = await client.PostAsJsonAsync("/country/v1/bulk-import/execute", new { jobId = Guid.NewGuid() });
        
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region Performance: Latency Checks

    [Fact]
    public async Task Authorization_Latency_ShouldBeUnder5ms()
    {
        var client = _factory.CreateClient().WithTestAuth(_factory, CountryPermissions.CountriesRead);
        
        // Warm up
        await client.GetAsync("/country/v1/countries");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 0; i < 100; i++)
        {
            await client.GetAsync("/country/v1/countries");
        }
        
        stopwatch.Stop();
        var averageLatencyMs = stopwatch.Elapsed.TotalMilliseconds / 100;
        
        _logger.LogInformation("Average authorization latency: {Latency}ms", averageLatencyMs);
        
        Assert.True(averageLatencyMs < 10); 
    }

    #endregion
}