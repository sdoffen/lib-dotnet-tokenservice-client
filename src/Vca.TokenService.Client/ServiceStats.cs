using System.Diagnostics.CodeAnalysis;

namespace Vca.TokenService.Client;

/// <summary>
/// Tracks statistics about token acquisition attempts.
/// </summary>
internal sealed class ServiceStats
{
    private int _failedAttempts;
    private readonly object _gate = new();

    /// <summary>
    /// Indicates whether a valid token is currently cached.
    /// </summary>
    public bool HasToken { get; private set; }

    /// <summary>
    /// Duration of the last successful token acquisition.
    /// </summary>
    public TimeSpan? LastAcquireDuration { get; private set; }

    /// <summary>
    /// Expiration time of the currently cached token.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>
    /// Timestamp of the last successful token acquisition.
    /// </summary>
    public DateTimeOffset? LastSuccessfulAcquireAt { get; private set; }

    /// <summary>
    /// Timestamp of the last token acquisition attempt.
    /// </summary>
    public DateTimeOffset? LastAcquireAttemptAt { get; private set; }

    /// <summary>
    /// Number of failed token acquisition attempts since the last successful acquisition.
    /// </summary>
    public int FailedAttempts => _failedAttempts;

    /// <summary>
    /// Marks that a token was found in the cache.
    /// </summary>
    public void OnCacheHit()
    {
        lock (_gate)
        {
            HasToken = true;
        }
    }

    /// <summary>
    /// Marks the start of a token acquisition attempt.
    /// </summary>
    public void OnAcquireAttemptStart()
    {
        lock (_gate)
        {
            LastAcquireAttemptAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Marks a successful token acquisition.
    /// </summary>
    /// <param name="duration">Duration of the token acquisition.</param>
    /// <param name="expiresAt">Expiration time of the acquired token.</param>
    public void OnAcquireSuccess(TimeSpan duration, DateTimeOffset expiresAt)
    {
        lock (_gate)
        {
            HasToken = true;
            LastAcquireDuration = duration;
            LastSuccessfulAcquireAt = DateTimeOffset.UtcNow;
            ExpiresAt = expiresAt;
        }
    }

    /// <summary>
    /// Marks a failed token acquisition.
    /// </summary>
    public void OnAcquireFailure(TimeSpan duration)
    {
        Interlocked.Increment(ref _failedAttempts);
        lock (_gate)
        {
            HasToken = false;
            ExpiresAt = null;
            LastAcquireDuration = duration;
        }
    }
}