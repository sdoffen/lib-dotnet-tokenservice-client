using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Vca.TokenService.Client.Tests;

public class ResilienceHandlerTests
{
    [Fact]
    public void ResilienceHandlerIsSet()
    {
        var configuration = new ConfigurationManager();
        configuration.AddJsonFile("appsettings.Test.json", optional: false);

        var tokenServiceOptions = configuration.GetSection("TokenService").Get<DefaultTokenServiceOptions>();
        tokenServiceOptions.ShouldNotBeNull();

        var services = new ServiceCollection();
        services.AddTokenServiceClient(configuration);

        var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(TokenServiceClientOptions.ClientName);

        var field = typeof(HttpMessageInvoker)
            .GetField("_handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var handler = (HttpMessageHandler?)field?.GetValue(client);
        handler.ShouldNotBeNull();

        bool found = ContainsHandler(handler, static t =>
                    t.FullName?.Contains("Microsoft.Extensions.Http.Resilience.ResilienceHandler", StringComparison.Ordinal) == true
                    || t.Name.Contains("ResilienceHandler", StringComparison.Ordinal));

        found.ShouldBeTrue("Expected the HttpClient handler chain to include ResilienceHandler after calling AddStandardResilienceHandler().");
    }

    private static bool ContainsHandler(HttpMessageHandler handler, Func<Type, bool> predicate)
    {
        var current = handler;
        while (current is DelegatingHandler d)
        {
            if (predicate(current.GetType()))
                return true;

            if (d.InnerHandler is null)
                return predicate(current.GetType());

            current = d.InnerHandler;
        }

        return predicate(current.GetType());
    }
}
