using Xunit;

namespace Maliev.CountryService.Tests.Fixtures;

/// <summary>
/// Separate collection for resilience tests that need to manipulate the database container.
/// These tests stop/start the PostgreSQL container, so they cannot share the container
/// with other tests.
/// </summary>
[CollectionDefinition("ResilienceTests", DisableParallelization = true)]
public class ResilienceTestCollection : ICollectionFixture<TestWebApplicationFactory>
{
}
