using Microsoft.Extensions.Diagnostics.HealthChecks;
using RoomBooking.Infrastructure.Persistence;

namespace RoomBooking.Api.Health;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(AppDbContext db, ILogger<DatabaseHealthCheck> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Database is reachable.")
                : HealthCheckResult.Unhealthy("Database is unreachable.");
        }
        catch (Exception ex)
        {
            // The details go to the log only, never to the caller of the endpoint.
            _logger.LogError(ex, "The database health check failed");
            return HealthCheckResult.Unhealthy("Database check failed.");
        }
    }
}