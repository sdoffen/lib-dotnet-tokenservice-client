namespace Vca.TokenService.Client.Tests;

public sealed class ServiceStatsRegistryTests
{
    [Fact]
    public void GetOrCreate_ReturnsSameInstance_ForSameKey()
    {
        var registry = new ServiceStatsRegistry();

        var a1 = registry.GetOrCreate("alpha");
        var a2 = registry.GetOrCreate("alpha");

        ReferenceEquals(a1, a2).ShouldBeTrue();
    }

    [Fact]
    public void GetOrCreate_ReturnsDifferentInstances_ForDifferentKeys()
    {
        var registry = new ServiceStatsRegistry();

        var a = registry.GetOrCreate("alpha");
        var b = registry.GetOrCreate("beta");

        ReferenceEquals(a, b).ShouldBeFalse();
    }

    [Fact]
    public void Snapshot_ContainsExistingEntries_WithSameReferences()
    {
        var registry = new ServiceStatsRegistry();

        var a = registry.GetOrCreate("alpha");
        var b = registry.GetOrCreate("beta");

        var snap = registry.Snapshot();

        snap.Count.ShouldBe(2);
        snap.Keys.ShouldBe(new[] { "alpha", "beta" }, ignoreOrder: true);
        snap["alpha"].ShouldBeSameAs(a);
        snap["beta"].ShouldBeSameAs(b);
    }

    [Fact]
    public void Snapshot_IsALiveView_ReflectsFutureAdds()
    {
        var registry = new ServiceStatsRegistry();

        registry.GetOrCreate("alpha");
        var snap = registry.Snapshot();
        snap.Count.ShouldBe(1);

        registry.GetOrCreate("beta");

        snap.Count.ShouldBe(2);
        snap.ContainsKey("beta").ShouldBeTrue();
    }

    [Fact]
    public async Task GetOrCreate_IsThreadSafe_SameKey_ReturnsSameInstance()
    {
        var registry = new ServiceStatsRegistry();
        const string key = "concurrent";
        const int N = 500;

        ServiceStats?[] results = new ServiceStats?[N];

        await Parallel.ForEachAsync(Enumerable.Range(0, N), CancellationToken.None,
            async (i, _) =>
            {
                results[i] = registry.GetOrCreate(key);
                await Task.Yield();
            });

        results.ShouldAllBe(r => r != null);
        var first = results[0]!;
        results.ShouldAllBe(r => ReferenceEquals(r, first));
        registry.Snapshot()[key].ShouldBeSameAs(first);
    }

    [Fact]
    public async Task GetOrCreate_IsThreadSafe_DistinctKeys_AllPresent()
    {
        var registry = new ServiceStatsRegistry();
        const int N = 200;

        var keys = Enumerable.Range(0, N).Select(i => $"key-{i}").ToArray();

        await Parallel.ForEachAsync(keys, CancellationToken.None,
            async (k, _) =>
            {
                registry.GetOrCreate(k);
                await Task.Yield();
            });

        var snap = registry.Snapshot();
        snap.Count.ShouldBe(N);
        foreach (var k in keys)
            snap.ContainsKey(k).ShouldBeTrue();
    }
}
