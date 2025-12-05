using System.Net;
using Xunit;
using Maliev.CountryService.Tests.Fixtures;

namespace Maliev.CountryService.Tests.Integration;

public class HealthCheckTests : IntegrationTestBase
{
    public HealthCheckTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Liveness_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/countries/liveness");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task Readiness_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/countries/readiness");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }
}
