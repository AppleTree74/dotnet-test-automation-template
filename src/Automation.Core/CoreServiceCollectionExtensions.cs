using Automation.Core.Artifacts;
using Automation.Core.Configuration;
using Automation.Core.Identity;
using Automation.Core.Logging;
using Automation.Core.Redaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Automation.Core;

/// <summary>
/// Registers the cross-cutting Core services: validated options, redaction, run identity,
/// artifact paths, and NLog-backed logging. Higher layers add their own capabilities on top.
/// </summary>
public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddAutomationCore(
        this IServiceCollection services,
        IConfiguration configuration,
        RunContext? runContext = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var environment = OptionsValidation.BindAndValidate<TestEnvironmentOptions>(configuration, TestEnvironmentOptions.SectionName);
        var browser = OptionsValidation.BindAndValidate<BrowserOptions>(configuration, BrowserOptions.SectionName);
        var api = OptionsValidation.BindAndValidate<ApiOptions>(configuration, ApiOptions.SectionName);
        var sql = OptionsValidation.BindAndValidate<SqlServerOptions>(configuration, SqlServerOptions.SectionName);
        var artifacts = OptionsValidation.BindAndValidate<ArtifactOptions>(configuration, ArtifactOptions.SectionName);
        var redaction = OptionsValidation.BindAndValidate<RedactionOptions>(configuration, RedactionOptions.SectionName);

        services.AddSingleton(environment);
        services.AddSingleton(browser);
        services.AddSingleton(api);
        services.AddSingleton(sql);
        services.AddSingleton(artifacts);
        services.AddSingleton(redaction);
        services.AddSingleton<IRedactor>(new Redactor(redaction));

        string repositoryRoot = RepositoryRoot.Find();
        var run = runContext ?? RunContext.Create();
        var paths = new ArtifactPaths(artifacts, run, repositoryRoot);
        ILoggerFactory loggerFactory = AutomationLogging.CreateFactory(run, paths.RunRoot);

        services.AddSingleton(run);
        services.AddSingleton(paths);
        services.AddSingleton(loggerFactory);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        return services;
    }
}
