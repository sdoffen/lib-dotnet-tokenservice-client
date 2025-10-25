using System.Diagnostics.CodeAnalysis;

namespace Vca.TokenService.Client;

[ExcludeFromCodeCoverage]
internal class TokenServiceClientResilienceOptions
{
    public static readonly string SectionName = "TokenService";

    /// <summary>
    /// The maximum attempted retries for calls to the token service.
    /// </summary>
    /// <remarks>Default value is 3.</remarks>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base value for the retry duration in milliseconds. The actual duration is calculated using this value with an exponential backoff strategy.
    /// </summary>
    /// <remarks>Default value is 2000.</remarks>
    public int RetryDelayMilliseconds { get; set; } = 2000;
}
