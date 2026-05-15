using System;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Data;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support.Auth;
using WebApp.IntegrationTests.Support.Extensions;

namespace WebApp.IntegrationTests.Support;

[Collection("Integration Tests")]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly HttpClient HttpClient;
    private readonly CustomWebApplicationFactory _factory;
    public TestUserContext UserContext => _factory.UserContext;
    
    protected BaseIntegrationTest(IntegrationTestFixture fixture)
    {
        _factory = fixture.Factory;
        HttpClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
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
    }

    protected static async Task<string> GetCsrfToken(HttpResponseMessage response)
    {
        var document = await response.ParseHtmlAsync();
        var token = document.QuerySelector("input[name='__RequestVerificationToken']") as IHtmlInputElement;
        return token?.Value ?? throw new InvalidOperationException("CSRF token not found in response");
    }
}