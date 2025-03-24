using Microsoft.Extensions.Diagnostics.HealthChecks;
using WebApplication1.Services;

namespace WebApplication1.HealthChecks;

public class DbMigrationHealthChecks : IHealthCheck
{
    private readonly DbMigrationService _dbMigration;

    public DbMigrationHealthChecks(DbMigrationService dbMigration)
    {
        _dbMigration = dbMigration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
    CancellationToken cancellationToken = default)
    {
        var task = _dbMigration.ExecuteTask;
        return task switch
        {
            { IsCompletedSuccessfully: true } => Task.FromResult(HealthCheckResult.Healthy("Database initialization completed successfully")),

            { IsFaulted: true } => Task.FromResult(HealthCheckResult.Unhealthy(task.Exception?.InnerException?.Message, task.Exception)),

            { IsCanceled: true } => Task.FromResult(HealthCheckResult.Unhealthy("Database initialization was canceled")),

            _ => Task.FromResult(HealthCheckResult.Degraded("Database initialization is still in progress"))
        };
    }
}
