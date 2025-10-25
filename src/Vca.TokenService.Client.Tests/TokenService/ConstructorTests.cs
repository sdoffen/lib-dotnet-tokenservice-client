using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Vca.TokenService.Client.Tests.TokenService;

public sealed class ConstructorTests
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

    public ConstructorTests()
    {
        _service = new TokenServiceClient<DefaultTokenServiceOptions>(_cacheMock.Object, _options, _factoryMock.Object, _statsRegistry, _logger);
    }

    [Fact]
    public void Ctor_Throws_WhenAnyParameterIsNull()
    {
        var cache = Mock.Of<IMemoryCache>();
        var options = Options.Create(new DefaultTokenServiceOptions());
        var factory = Mock.Of<IHttpClientFactory>();
        var logger = new NullLogger<TokenServiceClient<DefaultTokenServiceOptions>>();

        var ex1 = Should.Throw<ArgumentNullException>(() =>
        {
            _ = new TokenServiceClient<DefaultTokenServiceOptions>(null!, options, factory, _statsRegistry, logger);
        });
        ex1.ParamName.ShouldBe("cache");

        var ex2 = Should.Throw<ArgumentNullException>(() =>
        {
            _ = new TokenServiceClient<DefaultTokenServiceOptions>(cache, null!, factory, _statsRegistry, logger);
        });
        ex2.ParamName.ShouldBe("options");

        var ex3 = Should.Throw<ArgumentNullException>(() =>
        {
            _ = new TokenServiceClient<DefaultTokenServiceOptions>(cache, options, null!, _statsRegistry, logger);
        });
        ex3.ParamName.ShouldBe("factory");

        var ex4 = Should.Throw<ArgumentNullException>(() =>
        {
            _ = new TokenServiceClient<DefaultTokenServiceOptions>(cache, options, factory, null!, logger);
        });
        ex4.ParamName.ShouldBe("statsRegistry");

        var ex5 = Should.Throw<ArgumentNullException>(() =>
        {
            _ = new TokenServiceClient<DefaultTokenServiceOptions>(cache, options, factory, _statsRegistry, null!);
        });
        ex5.ParamName.ShouldBe("logger");
    }
}
