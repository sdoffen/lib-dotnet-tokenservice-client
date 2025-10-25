using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Vca.TokenService.Client;

/// <summary>
/// Extension methods for composing the token service client.
/// </summary>
public static class CompositionExtensions
{
    /// <summary>
    /// Adds the <see cref="ITokenServiceClient{T}"/> for <typeparamref name="T"/> to the service collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <remarks>
    /// T must be a subclass of <see cref="TokenServiceClientOptions"/>.
    /// </remarks>
    /// <returns></returns>
    public static IServiceCollection AddTokenServiceClient<T>(this IServiceCollection services, IConfiguration configuration)
        where T : TokenServiceClientOptions
    {
        // If the service has already been added, don't add it twice
        if (services.Any(descriptor => descriptor.ServiceType == typeof(TokenServiceClientMarker<T>)))
            return services;

        if (!services.Any(descriptor => descriptor.ServiceType == typeof(TokenServiceClientMarker)))
        {
            services.AddMemoryCache();

            services.AddHealthChecks()
                .AddCheck<TokenServiceClientHealthCheck>("Token Service");

            var options = configuration
                .GetSection(TokenServiceClientResilienceOptions.SectionName)
                .Get<TokenServiceClientResilienceOptions>()
                ?? new TokenServiceClientResilienceOptions();

            services.AddHttpClient(TokenServiceClientOptions.ClientName)
                .AddStandardResilienceHandler(o =>
                {
                    o.Retry.MaxRetryAttempts = Math.Max(1, options.RetryCount);
                    o.Retry.Delay = TimeSpan.FromMilliseconds(Math.Max(100, options.RetryDelayMilliseconds));
                    o.Retry.BackoffType = DelayBackoffType.Exponential;
                    o.Retry.UseJitter = true;
                });

            services.AddSingleton<IServiceStatsRegistry, ServiceStatsRegistry>();
            services.AddSingleton(new TokenServiceClientMarker());
        }

        services.AddOptions<T>()
            .Bind(configuration.GetSection("TokenService"))
            .Bind(configuration.GetSection(TokenServiceClientOptionsSectionName.For<T>()))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ITokenServiceClient<T>, TokenServiceClient<T>>();
        services.AddSingleton(new TokenServiceClientMarker<T>());
        return services;
    }

    /// <summary>
    /// Adds the <see cref="ITokenServiceClient{T}"/> for the default <see cref="DefaultTokenServiceOptions"/> to the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddTokenServiceClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ITokenServiceClient, TokenServiceClient<DefaultTokenServiceOptions>>();
        return services.AddTokenServiceClient<DefaultTokenServiceOptions>(configuration);
    }
}
