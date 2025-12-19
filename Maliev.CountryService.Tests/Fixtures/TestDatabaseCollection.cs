using Xunit;

namespace Maliev.CountryService.Tests.Fixtures;

[CollectionDefinition("TestDatabase")]
public class TestDatabaseCollection : ICollectionFixture<TestWebApplicationFactory>
{
}
