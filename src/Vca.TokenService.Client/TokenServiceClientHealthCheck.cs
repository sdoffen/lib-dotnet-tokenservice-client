using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Vca.TokenService.Client;

/// <summary>
/// Health check for the Token Service client.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class TokenServiceClientHealthCheck : IHealthCheck
{
    private readonly IServiceStatsRegistry _serviceStatsRegistry;

    public TokenServiceClientHealthCheck(IServiceStatsRegistry serviceStatsRegistry)
    {
        _serviceStatsRegistry = serviceStatsRegistry;
    }

    /// <summary>
    /// Checks the health of the Token Service client.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var snapshot = _serviceStatsRegistry.Snapshot();
        if (snapshot.Count == 0)
        {
            return Task.FromResult(HealthCheckResult.Degraded("No Token Service clients have been used yet."));
        }

        var isUnhealthy = false;
        var isDegraded = false;
        var data = new Dictionary<string, object>();
        var now = DateTimeOffset.UtcNow;

        foreach (var (key, stats) in snapshot)
        {
            var hasToken = stats.HasToken && stats.ExpiresAt is not null && stats.ExpiresAt > now;
            var entry = new Dictionary<string, object?>
            {
                ["hasToken"] = stats.HasToken,
                ["expiresAtUtc"] = stats.ExpiresAt,
                ["lastAcquireDurationMs"] = stats.LastAcquireDuration?.TotalMilliseconds,
                ["lastSuccessfulAcquireAtUtc"] = stats.LastSuccessfulAcquireAt,
                ["lastAcquireAttemptAtUtc"] = stats.LastAcquireAttemptAt,
                ["failedAttempts"] = stats.FailedAttempts
            };

            data[key] = entry;

            if (!hasToken) isUnhealthy = true;
            else if (stats.LastSuccessfulAcquireAt is null || stats.LastAcquireDuration?.TotalSeconds > 5)
            {
                isDegraded = true;
            }
        }

        if (isUnhealthy)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("One or more Token Service clients are unhealthy.", data: data));
        }
        else if (isDegraded)
        {
            return Task.FromResult(HealthCheckResult.Degraded("One or more Token Service clients are degraded.", data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy("All Token Service clients are healthy.", data: data));
    }
}
