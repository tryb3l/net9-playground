using Microsoft.Extensions.Diagnostics.HealthChecks;
using WebApp.Services;

namespace WebApp.HealthChecks;

public class DbMigrationHealthCheck : IHealthCheck
{
    private readonly DbMigrationService _migrationService;

    public DbMigrationHealthCheck(DbMigrationService migrationService)
    {
        _migrationService = migrationService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var task = _migrationService.ExecuteTask;

        return Task.FromResult(task.Status switch
        {
            TaskStatus.RanToCompletion => HealthCheckResult.Healthy("Database migration completed"),
            TaskStatus.Faulted => HealthCheckResult.Unhealthy(
                "Database migration failed",
                task.Exception?.InnerException ?? task.Exception),
            TaskStatus.Canceled => HealthCheckResult.Unhealthy("Database migration was canceled"),
            _ => HealthCheckResult.Unhealthy("Database migration is still in progress")
        });
    }
}