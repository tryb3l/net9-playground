using Xunit;

namespace WebApp.IntegrationTests;

[CollectionDefinition("Integration Tests")]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    // It ensures xUnit discovers and instantiates IntegrationTestFixture
}