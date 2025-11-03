using Microsoft.Extensions.DependencyInjection;
using WebApp.Data;

namespace WebApp.IntegrationTests;

[Collection("Integration Tests")]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly HttpClient HttpClient;
    private readonly CustomWebApplicationFactory _factory;
    private readonly TestOutputHelperAccessor _outputAccessor = new();

    protected BaseIntegrationTest(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _factory = fixture.Factory;
        _outputAccessor.SetOutput(output);
        _factory.SetMessageSink(_outputAccessor.GetEffectiveSink());
        HttpClient = _factory.HttpClient;
    }

    protected async Task<T> ExecuteDbContextAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(dbContext);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _factory.ResetDatabaseAsync();
        GC.SuppressFinalize(this);
    }
}