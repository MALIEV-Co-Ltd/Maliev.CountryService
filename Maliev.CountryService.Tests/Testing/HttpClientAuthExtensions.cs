using System.Net.Http.Headers;
using System.Security.Claims;
using Maliev.CountryService.Tests.Fixtures;

namespace Maliev.CountryService.Tests.Testing;

/// <summary>
/// Extension methods for HttpClient to support test authentication with permissions.
/// </summary>
public static class HttpClientAuthExtensions
{
    /// <summary>
    /// Adds a test JWT token with the specified permissions to the HttpClient request headers.
    /// </summary>
    /// <param name="client">The HttpClient instance</param>
    /// <param name="factory">The TestWebApplicationFactory to use for token generation</param>
    /// <param name="permissions">The permissions to include in the token</param>
    /// <returns>The HttpClient instance with the added Authorization header</returns>
    public static HttpClient WithTestAuth(this HttpClient client, TestWebApplicationFactory factory, params string[] permissions)
    {
        var token = factory.CreateTestJwtToken(
            userId: "test-admin",
            roles: null,
            permissions: permissions);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
