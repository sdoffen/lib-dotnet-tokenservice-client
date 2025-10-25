namespace Vca.TokenService.Client;

/// <summary>
/// Interface for a token service client.
/// </summary>
public interface ITokenServiceClient
{
    /// <summary>
    /// Gets an access token from the token service.
    /// </summary>
    /// <returns></returns>
    Task<string> GetAccessTokenAsync(CancellationToken token = default);
}

/// <summary>
/// Interface for a token service client.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ITokenServiceClient<T> : ITokenServiceClient
    where T : TokenServiceClientOptions
{
}
