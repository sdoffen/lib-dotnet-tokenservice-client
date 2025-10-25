using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Vca.TokenService.Client.Tests.TokenService;

public sealed class CalculateTokenExpirationTests
{
    private readonly Mock<IMemoryCache> _cacheMock = new();
    private readonly Mock<IHttpClientFactory> _factoryMock = new();
    private readonly NullLogger<TokenServiceClient<DefaultTokenServiceOptions>> _logger = new();
    private readonly IServiceStatsRegistry _statsRegistry = new ServiceStatsRegistry();
    private readonly IOptions<DefaultTokenServiceOptions> _options = Options.Create(new DefaultTokenServiceOptions
    {
        BasicAuth = "username:password",
        ServiceUrl = "https://token.service.local/"
    });

    private readonly TokenServiceClient<DefaultTokenServiceOptions> _service;

    public CalculateTokenExpirationTests()
    {
        _service = new TokenServiceClient<DefaultTokenServiceOptions>(_cacheMock.Object, _options, _factoryMock.Object, _statsRegistry, _logger);
    }

    [Theory]
    [InlineData(60, 60)]
    [InlineData(120, 60)]
    [InlineData(3600, 60)]
    public void CalculateTokenExpiration_UsesDefaultSkew_WhenExpiresInIsAtLeast60(int expiresIn, int expectedSkew)
    {
        var token = new AccessTokenResponseModel { AccessToken = "x", ExpiresIn = expiresIn };

        var before = DateTimeOffset.UtcNow;
        var actual = _service.CalculateTokenExpiration(token);
        var after = DateTimeOffset.UtcNow;

        var expected = before.AddSeconds(expiresIn - expectedSkew);

        // actual should be close to expected; allow 1s for clock drift across UtcNow calls
        (actual - expected).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(1));

        // and it should not regress earlier than "before" minus small tolerance
        actual.ShouldBeGreaterThanOrEqualTo(before.AddSeconds(expiresIn - expectedSkew - 1));
        actual.ShouldBeLessThanOrEqualTo(after.AddSeconds(expiresIn - expectedSkew + 1));
    }

    [Theory]
    [InlineData(59)]
    [InlineData(2)]
    [InlineData(1)]
    [InlineData(0)]
    public void CalculateTokenExpiration_UsesHalfSkew_WhenExpiresInIsLessThan60(int expiresIn)
    {
        var token = new AccessTokenResponseModel { AccessToken = "x", ExpiresIn = expiresIn };

        var before = DateTimeOffset.UtcNow;
        var actual = _service.CalculateTokenExpiration(token);
        var after = DateTimeOffset.UtcNow;

        var expectedSkew = expiresIn / 2;
        var expected = before.AddSeconds(expiresIn - expectedSkew);

        (actual - expected).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(1));
        actual.ShouldBeGreaterThanOrEqualTo(before.AddSeconds(expiresIn - expectedSkew - 1));
        actual.ShouldBeLessThanOrEqualTo(after.AddSeconds(expiresIn - expectedSkew + 1));
    }
}
