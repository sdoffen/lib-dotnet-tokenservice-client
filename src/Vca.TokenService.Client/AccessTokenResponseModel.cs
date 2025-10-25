using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Vca.TokenService.Client;

/// <summary>
/// Represents the response model for an request for an access token.
/// </summary>
[ExcludeFromCodeCoverage]
[DebuggerDisplay("AccessToken = {AccessToken}")]
internal sealed class AccessTokenResponseModel
{
    /// <summary>
    /// The access token.
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = null!;

    /// <summary>
    /// The number of seconds until the access token expires.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// The refresh token.
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = null!;

    /// <summary>
    /// The type of the token.
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = null!;
}
