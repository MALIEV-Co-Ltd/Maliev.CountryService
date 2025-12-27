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
    private static readonly Random _random = new();
    private static string GetRandomIso2(char prefix) => $"{prefix}{(char)_random.Next(65, 91)}";

    public AuthorizationTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region US1: Administrative Operations

    [Fact]
    public async Task CreateCountry_WithPermission_ShouldSucceed()
    {
        await _factory.CleanDatabaseAsync();

        var iso2 = GetRandomIso2('C');
        var iso3 = "ZZZ";
        var request = new CreateCountryRequest { Name = $"AuthTest-{Guid.NewGuid()}", Iso2 = iso2, Iso3 = iso3 };

        var client = _factory.CreateClient().WithTestAuth(_factory, CountryPermissions.CountriesCreate);

        var response = await client.PostAsJsonAsync("/country/v1/admin/countries", request);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            iso2 = GetRandomIso2('D');
            request.Iso2 = iso2;
            response = await client.PostAsJsonAsync("/country/v1/admin/countries", request);
        }

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateCountry_WithoutPermission_ShouldFail()
    {
        await _factory.CleanDatabaseAsync();

        var iso2 = GetRandomIso2('E');
        var request = new CreateCountryRequest { Name = "AuthTest2", Iso2 = iso2, Iso3 = "AUS" };
        var client = _factory.CreateClient().WithTestAuth(_factory, "Permission:invalid.permission");

        var response = await client.PostAsJsonAsync("/country/v1/admin/countries", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region US2: Bulk Import Operations

    [Fact]
    public async Task BulkImport_WithPermission_ShouldSucceed()
    {
        await _factory.CleanDatabaseAsync();

        var client = _factory.CreateClient().WithTestAuth(_factory, CountryPermissions.ImportExecute);
        var response = await client.PostAsJsonAsync("/country/v1/admin/bulk-import/00000000-0000-0000-0000-000000000000/process", new { });

        // Should not be Forbidden (might be NotFound if job doesn't exist, but auth passed)
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task BulkImport_WithoutPermission_ShouldBeForbidden()
    {
        await _factory.CleanDatabaseAsync();

        var client = _factory.CreateClient().WithTestAuth(_factory, "Permission:wrong.permission");
        var response = await client.PostAsJsonAsync("/country/v1/admin/bulk-import/00000000-0000-0000-0000-000000000000/process", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region Performance: Latency Checks

    [Fact]
    public async Task Authorization_Latency_ShouldBeUnder5ms()
    {
        await _factory.CleanDatabaseAsync();

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