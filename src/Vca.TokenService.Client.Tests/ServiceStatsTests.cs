namespace Vca.TokenService.Client.Tests;

public sealed class ServiceStatsTests
{
    [Fact]
    public void OnCacheHit_SetsHasTokenTrue_WhenCalled()
    {
        var sut = new ServiceStats();

        sut.HasToken.ShouldBeFalse();
        sut.ExpiresAt.ShouldBeNull();

        sut.OnCacheHit();

        sut.HasToken.ShouldBeTrue();
        sut.ExpiresAt.ShouldBeNull();
        sut.LastAcquireDuration.ShouldBeNull();
        sut.LastSuccessfulAcquireAt.ShouldBeNull();
        sut.LastAcquireAttemptAt.ShouldBeNull();
        sut.FailedAttempts.ShouldBe(0);
    }

    [Fact]
    public void OnAcquireAttemptStart_SetsLastAcquireAttemptAt_CloseToNow()
    {
        var sut = new ServiceStats();
        var before = DateTimeOffset.UtcNow;

        sut.OnAcquireAttemptStart();

        var after = DateTimeOffset.UtcNow;
        sut.LastAcquireAttemptAt.ShouldNotBeNull();
        sut.LastAcquireAttemptAt!.Value.ShouldBeInRange(before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public void OnAcquireSuccess_SetsFields_WhenCalled()
    {
        var sut = new ServiceStats();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
        var duration = TimeSpan.FromMilliseconds(123);
        var before = DateTimeOffset.UtcNow;

        sut.OnAcquireSuccess(duration, expiresAt);

        var after = DateTimeOffset.UtcNow;
        sut.HasToken.ShouldBeTrue();
        sut.LastAcquireDuration.ShouldBe(duration);
        sut.ExpiresAt.ShouldBe(expiresAt);
        sut.LastSuccessfulAcquireAt.ShouldNotBeNull();
        sut.LastSuccessfulAcquireAt!.Value.ShouldBeInRange(before.AddSeconds(-1), after.AddSeconds(1));
        sut.FailedAttempts.ShouldBe(0);
    }

    [Fact]
    public void OnAcquireFailure_IncrementsFailedAttempts_AndClearsTokenState()
    {
        var sut = new ServiceStats();
        sut.OnAcquireSuccess(TimeSpan.FromMilliseconds(50), DateTimeOffset.UtcNow.AddMinutes(5));

        sut.OnAcquireFailure(TimeSpan.FromMilliseconds(200));

        sut.HasToken.ShouldBeFalse();
        sut.ExpiresAt.ShouldBeNull();
        sut.LastAcquireDuration.ShouldBe(TimeSpan.FromMilliseconds(200));
        sut.FailedAttempts.ShouldBe(1);
    }

    [Fact]
    public void FailedAttempts_DoesNotReset_OnSuccess_WithCurrentImplementation()
    {
        var sut = new ServiceStats();

        sut.OnAcquireFailure(TimeSpan.FromMilliseconds(10));
        sut.OnAcquireFailure(TimeSpan.FromMilliseconds(10));
        sut.FailedAttempts.ShouldBe(2);

        sut.OnAcquireSuccess(TimeSpan.FromMilliseconds(20), DateTimeOffset.UtcNow.AddMinutes(1));

        // NOTE: Docs say "since the last successful acquisition",
        // but implementation does NOT reset the counter.
        sut.FailedAttempts.ShouldBe(2);
    }

    [Fact]
    public async Task OnAcquireFailure_IsThreadSafe_UnderConcurrency()
    {
        var sut = new ServiceStats();
        const int n = 500;

        await Parallel.ForEachAsync(Enumerable.Range(0, n), CancellationToken.None, async (_, _) =>
        {
            sut.OnAcquireFailure(TimeSpan.FromMilliseconds(1));
            await Task.Yield();
        });

        sut.FailedAttempts.ShouldBe(n);
        sut.HasToken.ShouldBeFalse();
        sut.ExpiresAt.ShouldBeNull();
    }

    [Fact]
    public void OnCacheHit_DoesNotModify_ExpirationOrTimestamps()
    {
        var sut = new ServiceStats();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(10);
        sut.OnAcquireSuccess(TimeSpan.FromMilliseconds(5), expiresAt);

        var lastSuccess = sut.LastSuccessfulAcquireAt;
        var lastDuration = sut.LastAcquireDuration;

        sut.OnCacheHit();

        sut.HasToken.ShouldBeTrue();
        sut.ExpiresAt.ShouldBe(expiresAt);
        sut.LastSuccessfulAcquireAt.ShouldBe(lastSuccess);
        sut.LastAcquireDuration.ShouldBe(lastDuration);
    }
}
