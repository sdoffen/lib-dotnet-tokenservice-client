using System.Diagnostics.CodeAnalysis;

namespace Vca.TokenService.Client;

/// <summary>
/// Exception thrown when a token request fails.
/// </summary>
[ExcludeFromCodeCoverage]
public class TokenServiceRequestException : Exception
{
    /// <summary>
    /// Creates a new instance of the <see cref="TokenServiceRequestException"/> class.
    /// </summary>
    /// <param name="message"></param>
    public TokenServiceRequestException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="TokenServiceRequestException"/> class with an inner exception.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public TokenServiceRequestException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
