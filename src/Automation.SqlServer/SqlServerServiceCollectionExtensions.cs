using Microsoft.Extensions.DependencyInjection;

namespace Automation.SqlServer;

/// <summary>
/// Registers the read-only SQL capability. The client is resolved lazily so an unconfigured
/// template (placeholder connection string) only fails when a Database test actually needs it.
/// Requires <c>AddAutomationCore</c>.
/// </summary>
public static class SqlServerServiceCollectionExtensions
{
    public static IServiceCollection AddAutomationSqlServer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient<IReadOnlySqlClient, ReadOnlySqlClient>();
        return services;
    }
}
