using Microsoft.Extensions.DependencyInjection;

namespace Automation.Browser;

/// <summary>Registers the browser capability. Requires <c>AddAutomationCore</c> to have run first.</summary>
public static class BrowserServiceCollectionExtensions
{
    public static IServiceCollection AddAutomationBrowser(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<PlaywrightDriver>();
        services.AddSingleton<IBrowserSessionFactory, BrowserSessionFactory>();
        return services;
    }
}
