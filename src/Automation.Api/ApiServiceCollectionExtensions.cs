using Automation.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Automation.Api;

/// <summary>
/// Registers the REST API capability: a typed <see cref="IApiClient"/> built on
/// <c>IHttpClientFactory</c> with a bearer delegating handler. Requires <c>AddAutomationCore</c>.
/// </summary>
public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddAutomationApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Default token provider reads the statically configured token. Products may replace
        // this registration with an OAuth/refresh implementation.
        services.AddSingleton<ITokenProvider>(sp =>
            new StaticTokenProvider(sp.GetRequiredService<ApiOptions>().BearerToken));

        services.AddTransient<BearerTokenHandler>();

        services.AddHttpClient<IApiClient, ApiClient>((sp, client) =>
            {
                ApiOptions options = sp.GetRequiredService<ApiOptions>();
                if (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out Uri? baseUri))
                {
                    client.BaseAddress = baseUri;
                }

                // Per-request timeout is enforced inside ApiClient; disable HttpClient's own timer
                // so timeouts surface as our sanitized RequestTimeout response, not a raw cancel.
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddHttpMessageHandler<BearerTokenHandler>();

        return services;
    }
}
