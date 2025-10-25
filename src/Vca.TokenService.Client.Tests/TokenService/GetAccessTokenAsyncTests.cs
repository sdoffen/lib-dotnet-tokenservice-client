using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Vca.TokenService.Client.Tests.TokenService;

public sealed class GetAccessTokenAsyncTests
{
    [Fact]
    public async Task GetAccessTokenAsync_ReturnsCachedToken_WhenCacheHit()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new PassThruHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = MakeJson(new AccessTokenResponseModel { AccessToken = "fresh", ExpiresIn = 3600 }) });
        var factory = CreateFactory(handler);
        var svc = CreateService(cache, factory);

        // Prime the cache
        var cacheKey = $"{typeof(DefaultTokenServiceOptions).Name}_AccessToken";
        cache.Set(cacheKey, new AccessTokenResponseModel { AccessToken = "cached", ExpiresIn = 3600 },
                  DateTimeOffset.UtcNow.AddMinutes(5));

        var token = await svc.GetAccessTokenAsync();

        token.ShouldBe("cached");
        handler.SendCount.ShouldBe(0); // no HTTP call on cache hit
    }

    [Fact]
    public async Task GetAccessTokenAsync_FetchesAndCaches_WhenCacheMiss()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new PassThruHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = MakeJson(new AccessTokenResponseModel { AccessToken = "fetched", ExpiresIn = 3600 })
        });
        var factory = CreateFactory(handler);
        var svc = CreateService(cache, factory);

        var token = await svc.GetAccessTokenAsync();

        token.ShouldBe("fetched");
        handler.SendCount.ShouldBe(1);

        // Verify it was cached
        cache.TryGetValue($"{typeof(DefaultTokenServiceOptions).Name}_AccessToken", out AccessTokenResponseModel? cached).ShouldBeTrue();
        cached!.AccessToken.ShouldBe("fetched");
    }

    [Fact]
    public async Task GetAccessTokenAsync_MultipleConcurrentCallers_PerformsSingleRequest_ThenUsesCache()
    {
        var gate = new TaskCompletionSource();
        var handler = new GateHandler(async () =>
        {
            await gate.Task; // block until released
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = MakeJson(new AccessTokenResponseModel { AccessToken = "concurrent", ExpiresIn = 3600 })
            };
        });

        var cache = new MemoryCache(new MemoryCacheOptions());
        var factory = CreateFactory(handler);
        var svc = CreateService(cache, factory);

        var c1 = svc.GetAccessTokenAsync();
        var c2 = svc.GetAccessTokenAsync();
        var c3 = svc.GetAccessTokenAsync();

        gate.SetResult(); // release the single in-flight HTTP
        var results = await Task.WhenAll(c1, c2, c3);

        results.ShouldAllBe(x => x == "concurrent");
        handler.SendCount.ShouldBe(1); // single HTTP due to SemaphoreSlim

        // Subsequent call should be cache hit (no extra HTTP)
        var next = await svc.GetAccessTokenAsync();
        next.ShouldBe("concurrent");
        handler.SendCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetAccessTokenAsync_UsesInnerCacheCheck_AfterEnteringLock()
    {
        // Simulate: first TryGetValue misses; once inside the lock, the cache becomes populated,
        // so RequestAccessTokenAsync must NOT be called.
        var cache = new MemoryCache(new MemoryCacheOptions());

        // Handler that would return something, but we’ll assert it wasn’t used.
        var handler = new PassThruHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = MakeJson(new AccessTokenResponseModel { AccessToken = "should-not-be-used", ExpiresIn = 3600 })
        });
        var factory = CreateFactory(handler);
        var svc = CreateService(cache, factory);

        // We coordinate via a separate task that races to populate the cache
        var cacheKey = $"{typeof(DefaultTokenServiceOptions).Name}_AccessToken";
        var gate = new TaskCompletionSource();

        // Kick off the call; while it waits to enter the lock, we populate the cache.
        var callTask = Task.Run(async () =>
        {
            // tiny delay so we are between outer TryGetValue and entering the locked section
            await Task.Delay(10);
            cache.Set(cacheKey, new AccessTokenResponseModel { AccessToken = "prepopulated", ExpiresIn = 3600 },
                      DateTimeOffset.UtcNow.AddMinutes(5));
            gate.SetResult();
        });

        // Wait for the cache population to be scheduled
        await gate.Task;

        var result = await svc.GetAccessTokenAsync();

        result.ShouldBe("prepopulated");
        handler.SendCount.ShouldBe(0); // RequestAccessTokenAsync not invoked
        await callTask;
    }

    [Fact]
    public async Task GetAccessTokenAsync_BubblesException_WhenRequestFails()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new ThrowingHandler(new InvalidOperationException("boom"));
        var factory = CreateFactory(handler);
        var svc = CreateService(cache, factory);

        var ex = await Should.ThrowAsync<TokenServiceRequestException>(() => svc.GetAccessTokenAsync());
        ex.InnerException.ShouldBeOfType<InvalidOperationException>();

        // After a failure, a subsequent success should still work (ensures lock released)
        var successHandler = new PassThruHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = MakeJson(new AccessTokenResponseModel { AccessToken = "ok", ExpiresIn = 3600 })
        });
        factory = CreateFactory(successHandler);
        svc = CreateService(cache, factory);

        var token = await svc.GetAccessTokenAsync();
        token.ShouldBe("ok");
        successHandler.SendCount.ShouldBe(1);
    }

    // ---------- helpers ----------

    private static IHttpClientFactory CreateFactory(HttpMessageHandler handler)
    {
        var mock = new Mock<IHttpClientFactory>();
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://token.example/") };
        mock.Setup(m => m.CreateClient(TokenServiceClientOptions.ClientName)).Returns(client);
        return mock.Object;
    }

    private static TokenServiceClient<DefaultTokenServiceOptions> CreateService(IMemoryCache cache, IHttpClientFactory factory)
        => new(cache,
               Options.Create(new DefaultTokenServiceOptions
               {
                   BasicAuth = "username:password",
                   ServiceUrl = "https://token.service.local/connect/token"
               }),
               factory,
               new ServiceStatsRegistry(),
               NullLogger<TokenServiceClient<DefaultTokenServiceOptions>>.Instance);

    private static StringContent MakeJson<T>(T payload)
        => new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    private sealed class PassThruHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public int SendCount { get; private set; }
        public PassThruHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.FromResult(_response);
        }
    }

    private sealed class GateHandler : HttpMessageHandler
    {
        private readonly Func<Task<HttpResponseMessage>> _factory;
        public int SendCount { get; private set; }
        public GateHandler(Func<Task<HttpResponseMessage>> factory) => _factory = factory;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;
            return await _factory();
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
