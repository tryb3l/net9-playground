using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;

namespace WebApp.Services;

public class DbMigrationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DbMigrationService> _logger;
    private readonly IHostEnvironment _environment;
    private readonly ActivitySource _activitySource = new("Database.Migrations");

    public new Task ExecuteTask => _executeTask?.Task ?? Task.CompletedTask;
    private readonly TaskCompletionSource? _executeTask;

    public DbMigrationService(
        IServiceProvider serviceProvider,
        ILogger<DbMigrationService> logger,
        IHostEnvironment environment)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _environment = environment;
        _executeTask = new TaskCompletionSource();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = _activitySource.StartActivity("Database Migration", ActivityKind.Client);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await InitializeDatabaseAsync(dbContext, stoppingToken);
            _executeTask?.SetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while migrating the database");
            _executeTask?.SetException(ex);
            throw;
        }
    }

    private async Task InitializeDatabaseAsync(ApplicationDbContext dbContext, CancellationToken stoppingToken)
    {
        using var activity = _activitySource.StartActivity("Initializing database", ActivityKind.Client);
        var stopwatch = Stopwatch.StartNew();

        if (!_environment.IsDevelopment())
        {
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(stoppingToken);
            var pendingList = pendingMigrations.ToList();

            if (pendingList.Count > 0)
            {
                _logger.LogWarning(
                    "Database has {Count} pending migrations: {Migrations}. " +
                    "Auto-migration is disabled in non-development environments. " +
                    "Run migrations via CI/CD pipeline or manually.",
                    pendingList.Count,
                    string.Join(", ", pendingList));
            }
            else
            {
                _logger.LogInformation("Database is up to date");
            }

            await SeedDataAsync(stoppingToken);
            return;
        }

        if (await dbContext.Database.CanConnectAsync(stoppingToken))
        {
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(stoppingToken);
            var pendingCount = pendingMigrations.Count();

            if (pendingCount > 0)
            {
                _logger.LogInformation("Applying {Count} pending migrations", pendingCount);
                await dbContext.Database.MigrateAsync(stoppingToken);
            }
            else
            {
                _logger.LogInformation("Database is up to date");
            }
        }
        else
        {
            _logger.LogInformation("Database does not exist, creating and applying migrations");
            await dbContext.Database.MigrateAsync(stoppingToken);
        }

        await SeedDataAsync(stoppingToken);

        _logger.LogInformation("Database initialization completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
    }

    private async Task SeedDataAsync(CancellationToken stoppingToken)
    {
        var disableSeeding = Environment.GetEnvironmentVariable("DISABLE_DB_SEEDING");
        if (disableSeeding == "true")
        {
            _logger.LogInformation("Database seeding is disabled");
            return;
        }

        _logger.LogInformation("Seeding database");
        await SeedData.InitializeAsync(_serviceProvider, stoppingToken);
    }
}