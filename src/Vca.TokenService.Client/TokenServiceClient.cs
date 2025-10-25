using System.Diagnostics;
using FluentHttpClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vca.TokenService.Client;

/// <summary>
/// Client for interacting with the Token Service.
/// </summary>
internal sealed class TokenServiceClient<T> : ITokenServiceClient<T> where T : TokenServiceClientOptions
{
    internal static readonly string UnableToRetrieveTokenMessage = "TokenServiceClient: Error when attempting to retrieve bearer token from {0}: {1}";

    private static readonly int _defaultSkewSeconds = 60;
    private static readonly string _cacheKey = $"{typeof(T).FullName}:AccessToken";
    private static readonly string _statsKey = $"{typeof(T).FullName}:ServiceStats";
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly IReadOnlyDictionary<string, string> _formContent = new Dictionary<string, string> { { "grant_type", "client_credentials" } };

    private readonly IMemoryCache _cache;
    private readonly T _options;
    private readonly IHttpClientFactory _factory;
    private readonly IServiceStatsRegistry _statsRegistry;
    private readonly ILogger<TokenServiceClient<T>> _logger;

    public TokenServiceClient(IMemoryCache cache, IOptions<T> options, IHttpClientFactory factory, IServiceStatsRegistry statsRegistry, ILogger<TokenServiceClient<T>> logger)
    {
        _cache = cache
            ?? throw new ArgumentNullException(nameof(cache));
        _options = options?.Value
            ?? throw new ArgumentNullException(nameof(options));
        _factory = factory
            ?? throw new ArgumentNullException(nameof(factory));
        _statsRegistry = statsRegistry
            ?? throw new ArgumentNullException(nameof(statsRegistry));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken token = default)
    {
        if (_cache.TryGetValue<AccessTokenResponseModel>(_cacheKey, out var accessToken) && accessToken is not null)
        {
            Stats().OnCacheHit();
            return accessToken.AccessToken;
        }

        await _lock.WaitAsync(token);
        try
        {
            if (_cache.TryGetValue<AccessTokenResponseModel>(_cacheKey, out accessToken) && accessToken is not null)
            {
                Stats().OnCacheHit();
                return accessToken.AccessToken;
            }

            var freshToken = await RequestAccessTokenAsync(token);
            var expiration = CalculateTokenExpiration(freshToken);

            _cache.Set(_cacheKey, freshToken, expiration);
            return freshToken.AccessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    internal async Task<AccessTokenResponseModel> RequestAccessTokenAsync(CancellationToken token)
    {
        var stats = Stats();
        stats.OnAcquireAttemptStart();
        var sw = Stopwatch.StartNew();

        try
        {
            using var client = _factory.CreateClient(TokenServiceClientOptions.ClientName);
            var request = client
                .UsingRoute(_options.ServiceUrl)
                .WithContent(new FormUrlEncodedContent(_formContent))
                .WithBasicAuthentication(_options.EncodedAuthToken);

            var response = await request.PostAsync(token);
            if (!response.IsSuccessStatusCode)
            {
                stats.OnAcquireFailure(sw.Elapsed);
                var error = $"Service returned response {(int)response.StatusCode} {response.ReasonPhrase}";
                throw new TokenServiceRequestException(string.Format(UnableToRetrieveTokenMessage, _options.ServiceUrl, error));
            }

            var tokenResponse = await response.DeserializeJsonAsync<AccessTokenResponseModel>();
            if (tokenResponse == null)
            {
                stats.OnAcquireFailure(sw.Elapsed);
                var error = $"Unable to deserialize response from token service.";
                throw new TokenServiceRequestException(string.Format(UnableToRetrieveTokenMessage, _options.ServiceUrl, error));
            }

            stats.OnAcquireSuccess(sw.Elapsed, CalculateTokenExpiration(tokenResponse));
            return tokenResponse;
        }
        catch (TokenServiceRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stats.OnAcquireFailure(sw.Elapsed);
            var error = $"Unexpected error occurred";
            _logger.LogError(ex, UnableToRetrieveTokenMessage, _options.ServiceUrl, error);
            throw new TokenServiceRequestException(string.Format(UnableToRetrieveTokenMessage, _options.ServiceUrl, error), ex);
        }
    }

    internal DateTimeOffset CalculateTokenExpiration(AccessTokenResponseModel token)
    {
        var skew = token.ExpiresIn < _defaultSkewSeconds ? token.ExpiresIn / 2 : _defaultSkewSeconds;
        return DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn - skew);
    }

    internal ServiceStats Stats() => _statsRegistry.GetOrCreate(_statsKey);
}
