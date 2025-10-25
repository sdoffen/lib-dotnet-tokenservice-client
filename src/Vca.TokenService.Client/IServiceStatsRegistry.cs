namespace Vca.TokenService.Client;

/// <summary>
/// Registry for service statistics.
/// </summary>
internal interface IServiceStatsRegistry
{
    /// <summary>
    /// Gets or creates service statistics for the specified key.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    ServiceStats GetOrCreate(string key);

    /// <summary>
    /// Creates a snapshot of the current service statistics.
    /// </summary>
    /// <returns></returns>
    IReadOnlyDictionary<string, ServiceStats> Snapshot();
}

