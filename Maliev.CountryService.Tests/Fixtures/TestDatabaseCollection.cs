using Xunit;

namespace Maliev.CountryService.Tests.Fixtures;

[CollectionDefinition("TestDatabase", DisableParallelization = true)]
public class TestDatabaseCollection : ICollectionFixture<TestWebApplicationFactory>
{
}
