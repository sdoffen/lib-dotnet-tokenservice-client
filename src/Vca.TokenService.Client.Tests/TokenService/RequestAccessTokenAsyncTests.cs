using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Vca.TokenService.Client.Tests.TokenService;

public sealed class RequestAccessTokenAsyncTests
{
    private readonly Mock<IMemoryCache> _cacheMock = new();
    private readonly Mock<IHttpClientFactory> _factoryMock = new();
    private readonly Mock<IServiceStatsRegistry> _statsRegistryMock = new();
    private readonly ILogger<TokenServiceClient<DefaultTokenServiceOptions>> _logger = NullLogger<TokenServiceClient<DefaultTokenServiceOptions>>.Instance;
    private readonly IOptions<DefaultTokenServiceOptions> _options = Options.Create(new DefaultTokenServiceOptions
    {
        BasicAuth = "username:password",
        ServiceUrl = "https://token.service.local/connect/token"
    });

    private TokenServiceClient<DefaultTokenServiceOptions> CreateService(ILogger<TokenServiceClient<DefaultTokenServiceOptions>>? logger = null)
        => new(_cacheMock.Object, _options, _factoryMock.Object, _statsRegistryMock.Object, logger ?? _logger);

    [Fact]
    public async Task RequestAccessTokenAsync_ReturnsModel_WhenSuccess200Json()
    {
        var token = Guid.NewGuid().ToString();
        var model = new AccessTokenResponseModel { AccessToken = token, ExpiresIn = 3600 };
        var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = MakeJson(model) });

        _factoryMock.Setup(f => f.CreateClient(TokenServiceClientOptions.ClientName))
                    .Returns(new HttpClient(handler));

        var svc = CreateService();
        var result = await svc.RequestAccessTokenAsync(CancellationToken.None);

        result.ShouldNotBeNull();
        result.AccessToken.ShouldBe(token);
        result.ExpiresIn.ShouldBe(3600);
        handler.SendCount.ShouldBe(1);
    }

    [Fact]
    public async Task RequestAccessTokenAsync_ThrowsTokenServiceRequestException_WhenStatusNotSuccess()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { ReasonPhrase = "Bad" };
        var handler = new StubHandler(response);
        _factoryMock.Setup(f => f.CreateClient(TokenServiceClientOptions.ClientName))
                    .Returns(new HttpClient(handler));

        var svc = CreateService();

        var ex = await Should.ThrowAsync<TokenServiceRequestException>(() => svc.RequestAccessTokenAsync(CancellationToken.None));

        var message = TokenServiceClient<DefaultTokenServiceOptions>.UnableToRetrieveTokenMessage;
        ex.Message.ShouldBe(string.Format(message, _options.Value.ServiceUrl, "Service returned response 400 Bad"));
        handler.SendCount.ShouldBe(1);
    }

    [Fact]
    public async Task RequestAccessTokenAsync_ThrowsTokenServiceRequestException_WhenDeserializeReturnsNull()
    {
        var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        });
        _factoryMock.Setup(f => f.CreateClient(TokenServiceClientOptions.ClientName))
                    .Returns(new HttpClient(handler));

        var svc = CreateService();

        var ex = await Should.ThrowAsync<TokenServiceRequestException>(() => svc.RequestAccessTokenAsync(CancellationToken.None));

        ex.Message.ShouldContain("Unable to deserialize response from token service.");
        handler.SendCount.ShouldBe(1);
    }

    [Fact]
    public async Task RequestAccessTokenAsync_LogsAndWraps_WhenUnexpectedException()
    {
        var inner = new InvalidOperationException("boom");
        var handler = new ThrowingHandler(inner);
        _factoryMock.Setup(f => f.CreateClient(TokenServiceClientOptions.ClientName))
                    .Returns(new HttpClient(handler));

        var svc = CreateService();

        var ex = await Should.ThrowAsync<TokenServiceRequestException>(() => svc.RequestAccessTokenAsync(CancellationToken.None));

        ex.InnerException.ShouldBeOfType<InvalidOperationException>();
        ex.InnerException!.Message.ShouldBe("boom");

        handler.SendCount.ShouldBe(1);
    }

    // ---------- helpers ----------

    private static StringContent MakeJson<T>(T payload)
        => new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public int SendCount { get; private set; }
        public StubHandler(HttpResponseMessage response) => _response = response;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.FromResult(_response);
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        private readonly Exception _toThrow;
        public int SendCount { get; private set; }
        public ThrowingHandler(Exception ex) => _toThrow = ex;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.FromException<HttpResponseMessage>(_toThrow);
        }
    }
}
