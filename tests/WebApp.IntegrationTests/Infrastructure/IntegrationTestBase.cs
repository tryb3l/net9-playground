using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
{
    private readonly IntegrationTestWebAppFactory _factory;
    private readonly HttpClient _client;
    private Respawner _respawner = null!;
    private DbConnection? _dbConnection;

    protected IntegrationTestBase(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true
        });
    }

    public async ValueTask InitializeAsync()
    {
        // Initialize Respawn to reset DB between tests
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var connectionString = dbContext.Database.GetConnectionString();

        if (connectionString != null)
        {
            _dbConnection = new NpgsqlConnection(connectionString);
            await _dbConnection.OpenAsync();

            _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                TablesToIgnore = ["__EFMigrationsHistory"]
            });
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_dbConnection != null)
        {
            await _respawner.ResetAsync(_dbConnection);
        }
        
        if (_dbConnection != null)
        {
            await _dbConnection.DisposeAsync();
        }
    }
    
    private static async Task<string> GetCsrfToken(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(content);
        var token = document.QuerySelector("input[name='__RequestVerificationToken']") as IHtmlInputElement;
        return token?.Value ?? throw new Exception("CSRF token not found in response");
    }
    
    protected async Task AuthenticateAsync(string username = "admin", string password = "Password123!")
    {
        // Seed User
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            var user = await userManager.FindByNameAsync(username);
            if (user == null)
            {
                user = new User { UserName = username, Email = username + "@example.com", EmailConfirmed = true };
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }
        }

        // Perform Login Request to get Cookies
        var loginPage = await _client.GetAsync("/Identity/Account/Login");
        var csrfToken = await GetCsrfToken(loginPage);

        var loginData = new Dictionary<string, string>
        {
            { "Input.Email", username + "@example.com" },
            { "Input.Password", password },
            { "__RequestVerificationToken", csrfToken }
        };

        var response = await _client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(loginData));
        response.EnsureSuccessStatusCode();
    }
}