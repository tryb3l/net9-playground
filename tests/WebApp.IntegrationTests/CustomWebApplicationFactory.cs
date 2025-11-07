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
using Xunit.Sdk;

namespace WebApp.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:18-alpine")
        .WithDatabase("test_db")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private NpgsqlConnection? _dbConnection;
    private Respawner? _respawner;
    public HttpClient HttpClient { get; private set; } = null!;
    private readonly TestOutputHelperAccessor _loggerAccessor = new();

    public void SetMessageSink(IMessageSink? messageSink) => _loggerAccessor.MessageSink = messageSink;

    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("CONNECTION_STRING", _dbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("GOOGLE_CLIENT_ID", "test-client-id");
        Environment.SetEnvironmentVariable("GOOGLE_CLIENT_SECRET", "test-client-secret");
        Environment.SetEnvironmentVariable("HEALTHCHECKS_API_KEY", "test-key");
        Environment.SetEnvironmentVariable("DISABLE_DB_SEEDING", "true");

        var logger = Services.GetRequiredService<ILogger<LoggingHttpMessageHandler>>();
        HttpClient = CreateDefaultClient(new LoggingHttpMessageHandler(logger));

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
            logging.AddXUnit(_loggerAccessor);
            logging.SetMinimumLevel(LogLevel.Information);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IHostedService>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));
        });
    }

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