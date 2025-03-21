using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApplication1.Data;

namespace WebApplication1.Services;

public class DbMigrationService
{
    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<DbMigrationService>>();

            try
            {
                if (context.Database.CanConnect())
                {
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                    var pendingCount = pendingMigrations.Count();

                    if (pendingCount > 0)
                    {
                        logger.LogInformation("Applying {Count} pending migrations", pendingCount);
                        await context.Database.MigrateAsync();
                    }
                    else
                    {
                        logger.LogInformation("Database is up to date, no migrations to apply");
                    }
                }
                else
                {
                    logger.LogInformation("Database does not exist yet, creating and applying migrations");
                    await context.Database.MigrateAsync();
                }

                await SeedData.Initialize(services);
            }
            catch (Exception ex) when (ex.Message.Contains("relation") && ex.Message.Contains("already exists"))
            {
                // This specific exception occurs when tables exist but migration history is missing
                logger.LogWarning("Tables already exist but migration history is incomplete. Attempting to repair...");

                try
                {
                    await RepairMigrationHistoryAsync(context, logger);
                }
                catch (Exception repairEx)
                {
                    logger.LogError(repairEx, "Failed to repair migration history");
                }
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<DbMigrationService>>();
            logger.LogError(ex, "An error occurred during database initialization");
        }
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
            .OrderBy(m => m.Migration!.Id);

        logger.LogInformation("Attempting to mark {Count} migrations as applied", migrations.Count());

        foreach (var migration in migrations)
        {
            var id = migration.Migration!.Id;
            var applied = await context.Database.GetAppliedMigrationsAsync();

            if (!applied.Contains(id))
            {
                logger.LogInformation("Marking migration {Id} as applied", id);

                // Get version and truncate it if necessary
                var version = migration.Type.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "9.0.0";
                if (version.Length > 32)
                {
                    // Only keep the first 32 characters or just the version without the hash
                    version = version.Split('+')[0];
                }

                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1})",
                    id,
                    version);
            }
        }
    }
}