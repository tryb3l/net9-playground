using Microsoft.Extensions.DependencyInjection;
using WebApp.Data;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support.Auth;
using WebApp.IntegrationTests.Support.Logging;

namespace WebApp.IntegrationTests.Support;

[Collection("Integration Tests")]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly HttpClient HttpClient;
    private readonly CustomWebApplicationFactory _factory;
    private readonly TestOutputHelperAccessor _outputAccessor = new();
    public TestUserContext UserContext => _factory.UserContext;
    
    protected BaseIntegrationTest(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _factory = fixture.Factory;
        _outputAccessor.SetOutput(output);
        _factory.SetMessageSink(_outputAccessor.GetEffectiveSink());
        HttpClient = _factory.HttpClient;
        UserContext.CurrentUser = null;
    }

    public async Task<T> ExecuteDbContextAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(dbContext);
    }
    
    public async Task ExecuteDbContextAsync(Func<ApplicationDbContext, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await action(dbContext);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _factory.ResetDatabaseAsync();
        GC.SuppressFinalize(this);
    }
}