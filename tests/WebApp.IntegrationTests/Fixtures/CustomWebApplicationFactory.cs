using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using WebApp.Data;
using WebApp.IntegrationTests.Support.Auth;

namespace WebApp.IntegrationTests.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("test_db")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private NpgsqlConnection? _dbConnection;
    private Respawner? _respawner;
    public HttpClient HttpClient { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("CONNECTION_STRING", _dbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("GOOGLE_CLIENT_ID", "test-client-id");
        Environment.SetEnvironmentVariable("GOOGLE_CLIENT_SECRET", "test-client-secret");
        Environment.SetEnvironmentVariable("HEALTHCHECKS_API_KEY", "test-key");
        Environment.SetEnvironmentVariable("DISABLE_DB_SEEDING", "true");

        HttpClient = CreateDefaultClient();

        using (var scope = Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        _dbConnection = new NpgsqlConnection(_dbContainer.GetConnectionString());
        await _dbConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore =
            [
                "__EFMigrationsHistory",
                "AspNetRoles",
                "AspNetUsers",
                "AspNetUserRoles",
                "AspNetUserClaims",
                "AspNetRoleClaims",
                "AspNetUserLogins",
                "AspNetUserTokens"
            ]
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TestUserContext>();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultScheme = TestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme, options => { });
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IHostedService>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));
        });
    }

    public TestUserContext UserContext => Services.GetRequiredService<TestUserContext>();

    public async Task ResetDatabaseAsync()
    {
        if (_respawner != null && _dbConnection != null)
            await _respawner.ResetAsync(_dbConnection);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_dbConnection != null)
            await _dbConnection.DisposeAsync();

        await _dbContainer.DisposeAsync();
        GC.SuppressFinalize(this);
        await base.DisposeAsync();
    }
}