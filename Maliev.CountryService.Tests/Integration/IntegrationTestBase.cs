using System.Text.Json; // Added for JsonSerializerOptions
using Maliev.CountryService.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Maliev.CountryService.Tests.Integration;

public abstract class IntegrationTestBase : IClassFixture<TestWebApplicationFactory>
{
    protected readonly TestWebApplicationFactory _factory;
    protected readonly HttpClient _client;
    protected readonly JsonSerializerOptions JsonSerializerOptions; // Added
    protected readonly ILogger _logger;

    protected IntegrationTestBase(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        JsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }; // Initialized
        _logger = factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
    }

    protected HttpClient CreateAdminClient(string userId = "testuser", string role = "admin")
    {
        var adminToken = _factory.GenerateTestToken(userId, role);
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");
        return adminClient;
    }
}
