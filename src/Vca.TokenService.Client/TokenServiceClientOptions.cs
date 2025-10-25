using System.Text;

namespace Vca.TokenService.Client;

/// <summary>
/// Options used for configuring the token service client.
/// </summary>
public abstract class TokenServiceClientOptions
{
    internal static string ClientName { get; } = $"TokenServiceClient-{Guid.NewGuid()}";

    private string? _encodedAuthToken;
    private string? _basicAuth;

    /// <summary>
    /// Gets the basic token used to request a bearer token from the token service.
    /// </summary>
    public string BasicAuth
    {
        get => _basicAuth!;
        set
        {
            _basicAuth = value;
            _encodedAuthToken = null; // reset cached value
        }
    }

    /// <summary>
    /// Gets the base64 encoded basic authentication token.
    /// </summary>
    public string EncodedAuthToken
    {
        get
        {
            if (_encodedAuthToken == null)
            {
                _encodedAuthToken = Convert.ToBase64String(Encoding.ASCII.GetBytes(BasicAuth));
            }

            return _encodedAuthToken!;
        }
    }

    /// <summary>
    /// Gets or sets the url for the token service.
    /// </summary>
    public string ServiceUrl { get; set; } = null!;
}
