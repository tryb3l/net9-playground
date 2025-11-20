namespace WebApp.IntegrationTests.Fixtures;

[CollectionDefinition("Integration Tests")]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    // It ensures xUnit discovers and instantiates IntegrationTestFixture
}