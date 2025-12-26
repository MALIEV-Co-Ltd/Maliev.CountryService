using System.Text.Json;
using Maliev.CountryService.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Maliev.CountryService.Tests.Integration;

/// <summary>
/// Base class for integration tests. Tests using this base class should be marked with [Collection("TestDatabase")]
/// to share the test factory and avoid creating multiple Docker containers.
/// </summary>
public abstract class IntegrationTestBase
{
    protected readonly TestWebApplicationFactory _factory;
    protected readonly HttpClient _client;
    protected readonly JsonSerializerOptions JsonSerializerOptions;
    protected readonly ILogger _logger;

    protected IntegrationTestBase(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        JsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _logger = factory.Services.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
    }

    protected static readonly string[] CountryAdminRoles = { "roles.country.admin" };
    protected static readonly string[] SuperAdminRoles = { "roles.country.superadmin" };
}
