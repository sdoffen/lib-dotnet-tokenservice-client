using System.Collections.Concurrent;

namespace Vca.TokenService.Client;

internal sealed class ServiceStatsRegistry : IServiceStatsRegistry
{
    private readonly ConcurrentDictionary<string, ServiceStats> _map = new();

    public ServiceStats GetOrCreate(string key)
    {
        return _map.GetOrAdd(key, _ => new ServiceStats());
    }

    public IReadOnlyDictionary<string, ServiceStats> Snapshot()
    {
        return _map;
    }
}
