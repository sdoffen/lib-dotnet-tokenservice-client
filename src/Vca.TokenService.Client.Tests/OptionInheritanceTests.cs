using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Vca.TokenService.Client.Tests;

public class OptionInheritanceTests
{
    [Fact]
    public void OptionsShouldInheritDefaultValues()
    {
        var configuration = new ConfigurationManager();
        configuration.AddJsonFile("appsettings.Test.json", optional: false);

        var tokenServiceOptions = configuration.GetSection("TokenService").Get<DefaultTokenServiceOptions>();
        tokenServiceOptions.ShouldNotBeNull();

        var services = new ServiceCollection();
        services.AddTokenServiceClient<InheritedOptions>(configuration);
        services.AddTokenServiceClient<OverrideOptions>(configuration);
        services.AddTokenServiceClient<PartialOverrideOptions>(configuration);

        // Add this twice to ensure that multiple registrations do not interfere with each other
        services.AddTokenServiceClient<PartialOverrideOptions>(configuration);

        var serviceProvider = services.BuildServiceProvider();

        var inheritedOptions = serviceProvider.GetRequiredService<IOptions<InheritedOptions>>().Value;
        inheritedOptions.ShouldNotBeNull();
        inheritedOptions.BasicAuth.ShouldBe(tokenServiceOptions.BasicAuth);
        inheritedOptions.ServiceUrl.ShouldBe(tokenServiceOptions.ServiceUrl);

        var overrideOptions = serviceProvider.GetRequiredService<IOptions<OverrideOptions>>().Value;
        overrideOptions.ShouldNotBeNull();
        overrideOptions.BasicAuth.ShouldNotBeNull();
        overrideOptions.BasicAuth.ShouldNotBe(tokenServiceOptions.BasicAuth);
        overrideOptions.ServiceUrl.ShouldNotBe(tokenServiceOptions.ServiceUrl);

        var partialOverrideOptions = serviceProvider.GetRequiredService<IOptions<PartialOverrideOptions>>().Value;
        partialOverrideOptions.ShouldNotBeNull();
        partialOverrideOptions.BasicAuth.ShouldNotBeNull();
        partialOverrideOptions.ServiceUrl.ShouldNotBeNull(tokenServiceOptions.BasicAuth);
        partialOverrideOptions.ServiceUrl.ShouldBe(tokenServiceOptions.ServiceUrl);

        var ts1 = serviceProvider.GetRequiredService<ITokenServiceClient<InheritedOptions>>();
        ts1.ShouldNotBeNull();

        var ts2 = serviceProvider.GetRequiredService<ITokenServiceClient<OverrideOptions>>();
        ts2.ShouldNotBeNull();

        var ts3 = serviceProvider.GetRequiredService<ITokenServiceClient<PartialOverrideOptions>>();
        ts3.ShouldNotBeNull();

        var defaultOptions = serviceProvider.GetRequiredService<IOptions<DefaultTokenServiceOptions>>().Value;
        defaultOptions.ShouldNotBeNull();
        defaultOptions.BasicAuth.ShouldBeNull();
        defaultOptions.ServiceUrl.ShouldBeNull();

        var tn = serviceProvider.GetService<ITokenServiceClient>();
        tn.ShouldBeNull();
    }

    [Fact]
    public void DefaultOptionsShouldBeUsedWhenNoGenericParameterProvided()
    {
        var configuration = new ConfigurationManager();
        configuration.AddJsonFile("appsettings.Test.json", optional: false);

        var services = new ServiceCollection();
        services.AddTokenServiceClient(configuration);

        var serviceProvider = services.BuildServiceProvider();

        var defaultOptions = serviceProvider.GetRequiredService<IOptions<DefaultTokenServiceOptions>>().Value;
        defaultOptions.ShouldNotBeNull();
        defaultOptions.BasicAuth.ShouldNotBeNull();
        defaultOptions.ServiceUrl.ShouldNotBeNull();

        var tn = serviceProvider.GetService<ITokenServiceClient>();
        tn.ShouldNotBeNull();
    }

    [TokenServiceClientOptionsSectionName("InheritedOptions")]
    internal class InheritedOptions : TokenServiceClientOptions { }

    [TokenServiceClientOptionsSectionName("OverrideOptions")]
    internal class OverrideOptions : TokenServiceClientOptions { }

    [TokenServiceClientOptionsSectionName("PartialOverrideOptions")]
    internal class PartialOverrideOptions : TokenServiceClientOptions
    {
    }
}
