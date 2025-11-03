using System.Threading.Tasks;
using Xunit;

namespace WebApp.IntegrationTests;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    public CustomWebApplicationFactory Factory { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        Factory = new CustomWebApplicationFactory();
        await Factory.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
    }
}