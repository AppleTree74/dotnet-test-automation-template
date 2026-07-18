using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Automation.Core.Configuration;

/// <summary>
/// Builds configuration with the documented precedence (guide section 11), lowest to highest:
/// committed <c>appsettings.json</c>, local .NET user-secrets, environment variables
/// (GitHub Environment values map here), then explicit overrides.
/// </summary>
public static class TestConfiguration
{
    /// <summary>Environment variable prefix for CI/host overrides, e.g. <c>AUTOMATION__Api__BaseUrl</c>.</summary>
    public const string EnvironmentPrefix = "AUTOMATION__";

    public static IConfigurationRoot Build(
        string? basePath = null,
        Assembly? userSecretsAssembly = null,
        IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath ?? AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false);

        Assembly secretsAssembly = userSecretsAssembly ?? Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        builder.AddUserSecrets(secretsAssembly, optional: true);

        builder.AddEnvironmentVariables(EnvironmentPrefix);

        if (overrides is { Count: > 0 })
        {
            builder.AddInMemoryCollection(overrides);
        }

        return builder.Build();
    }
}
