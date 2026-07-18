using Application.Automation.Api;
using Application.Automation.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Automation;

/// <summary>Registers product automation types (typed clients, workflows) on top of the framework.</summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationAutomation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient<OrdersApiClient>();
        services.AddTransient<CustomerJourney>();
        return services;
    }
}
