using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApplication1.Data;

namespace WebApplication1.Services;

public class DbMigrationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DbMigrationService> _logger;
    private readonly ActivitySource _activitySource = new("Database.Migrations");

    public new Task ExecuteTask => _executeTask?.Task ?? Task.CompletedTask;
    private readonly TaskCompletionSource? _executeTask;

    public DbMigrationService(
        IServiceProvider serviceProvider,
        ILogger<DbMigrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _executeTask = new TaskCompletionSource();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(1000, stoppingToken);

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await InitializeDatabaseAsync(dbContext, stoppingToken);
            _executeTask?.SetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during database initialization");
            _executeTask?.SetException(ex);
            throw;
        }
    }

    private async Task InitializeDatabaseAsync(ApplicationDbContext dbContext, CancellationToken stoppingToken)
    {
        using var activity = _activitySource.StartActivity("Initializing database", ActivityKind.Client);
        var stopwatch = Stopwatch.StartNew();

        try
        {
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
                    _logger.LogInformation("Database is up to date, no migrations to apply");
                }
            }
            else
            {
                _logger.LogInformation("Database does not exist yet, creating and applying migrations");
                await dbContext.Database.MigrateAsync(stoppingToken);
            }

            await SeedDataAsync(stoppingToken);

            _logger.LogInformation("Database initialization completed after {ElapsedMilliseconds}ms",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex.Message.Contains("relation") && ex.Message.Contains("already exists"))
        {
            // This specific exception occurs when tables exist but migration history is missing
            _logger.LogWarning("Tables already exist but migration history is incomplete. Attempting to repair...");

            try
            {
                await RepairMigrationHistoryAsync(dbContext, _logger);
                await SeedDataAsync(stoppingToken);
                _logger.LogInformation("Database repair and seeding completed after {ElapsedMilliseconds}ms",
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception repairEx)
            {
                _logger.LogError(repairEx, "Failed to repair migration history");
                throw;
            }
        }
    }

    private async Task SeedDataAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Seeding database");
        using var scope = _serviceProvider.CreateScope();
        await SeedData.Initialize(scope.ServiceProvider, stoppingToken);
    }

    private static async Task RepairMigrationHistoryAsync(ApplicationDbContext context, ILogger logger)
    {
        var migrations = context.GetType().Assembly
            .GetTypes()
            .Where(t => t.IsClass && t.GetCustomAttribute<MigrationAttribute>() != null)
            .Select(t => new
            {
                Migration = t.GetCustomAttribute<MigrationAttribute>(),
                Type = t
            })
            .OrderBy(m => m.Migration!.Id)
            .ToList();

        logger.LogInformation("Attempting to mark {Count} migrations as applied", migrations.Count);

        foreach (var migration in migrations)
        {
            var id = migration.Migration!.Id;
            var applied = await context.Database.GetAppliedMigrationsAsync();

            if (applied.Contains(id)) continue;
            logger.LogInformation("Marking migration {Id} as applied", id);

            var version = migration.Type.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "9.0.0";
            if (version.Length > 32)
            {
                // Only keep the first part without commit hash
                version = version.Split('+')[0];
            }

            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1})",
                id,
                version);
        }
    }
}