using Application.Automation;
using Automation.Api;
using Automation.Browser;
using Automation.Core;
using Automation.Core.Artifacts;
using Automation.Core.Configuration;
using Automation.Core.Diagnostics;
using Automation.Core.Identity;
using Automation.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests.Framework;

/// <summary>
/// Process-wide run state shared by every fixture: the service provider, run identity, artifact
/// paths, and selected browser. Initialized once by <see cref="GlobalSetup"/>.
/// </summary>
public static class TestRun
{
    private static ServiceProvider? _services;
    private static int _failures;
    private static int _total;

    public static IServiceProvider Services =>
        _services ?? throw new InvalidOperationException("TestRun has not been initialized. Is GlobalSetup registered?");

    public static RunContext Run { get; private set; } = null!;

    public static ArtifactPaths Paths { get; private set; } = null!;

    public static BrowserKind SelectedBrowser { get; private set; } = BrowserKind.Chromium;

    public static string RepositoryRootPath { get; private set; } = string.Empty;

    public static void Initialize()
    {
        RepositoryRootPath = RepositoryRoot.Find();

        // Pin the working directory so Allure's results directory and the artifacts tree both
        // resolve to the repository root regardless of how the test host was launched.
        Directory.SetCurrentDirectory(RepositoryRootPath);

        IConfiguration configuration = TestConfiguration.Build(
            basePath: AppContext.BaseDirectory,
            userSecretsAssembly: typeof(TestRun).Assembly);

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddAutomationCore(configuration);
        services.AddAutomationBrowser();
        services.AddAutomationApi();
        services.AddAutomationSqlServer();
        services.AddApplicationAutomation();

        _services = services.BuildServiceProvider();
        Run = _services.GetRequiredService<RunContext>();
        Paths = _services.GetRequiredService<ArtifactPaths>();
        SelectedBrowser = ResolveBrowser(_services.GetRequiredService<BrowserOptions>());

        // Clean Allure results before this independent launch (guide section 14). When a launcher
        // runs several browsers into one report (scripts/Invoke-Tests.ps1 -Browser all), it cleans
        // once up front and sets AUTOMATION_KEEP_ALLURE_RESULTS so each browser's results accumulate
        // instead of the next launch deleting the previous browser's results.
        if (!KeepAllureResults() && Directory.Exists(Paths.AllureResultsDirectory))
        {
            Directory.Delete(Paths.AllureResultsDirectory, recursive: true);
        }

        Paths.EnsureRunDirectories();
    }

    private static bool KeepAllureResults() =>
        string.Equals(Environment.GetEnvironmentVariable("AUTOMATION_KEEP_ALLURE_RESULTS"), "1", StringComparison.Ordinal);

    public static void RecordOutcome(bool passed)
    {
        Interlocked.Increment(ref _total);
        if (!passed)
        {
            Interlocked.Increment(ref _failures);
        }
    }

    public static async Task ShutdownAsync()
    {
        WriteManifest();

        if (_services is not null)
        {
            await _services.DisposeAsync().ConfigureAwait(false);
            _services = null;
        }
    }

    private static void WriteManifest()
    {
        string result = _total == 0 ? "incomplete" : _failures == 0 ? "passed" : "failed";
        string? testName = Environment.GetEnvironmentVariable("AUTOMATION_TEST_NAME");
        bool byTestName = !string.IsNullOrWhiteSpace(testName);

        RunManifest manifest = RunManifestWriter.Build(
            Run,
            Paths,
            type: Environment.GetEnvironmentVariable("AUTOMATION_TYPE") ?? "all",
            suite: Environment.GetEnvironmentVariable("AUTOMATION_SUITE") ?? "all",
            browser: ManifestBrowserLabel(),
            workers: TestContextWorkers(),
            result: result,
            completedUtc: DateTimeOffset.UtcNow,
            selectionMode: byTestName ? "test-name" : "category",
            testName: byTestName ? testName : null);

        RunManifestWriter.Write(Paths.RunManifestPath, manifest);
    }

    /// <summary>
    /// The browser to record in the manifest. A browser-free selection (API/Database, signalled by
    /// an unparseable <c>AUTOMATION_BROWSER</c> such as <c>not-applicable</c>) records
    /// <c>not-applicable</c> rather than a browser that was never launched (P2-04).
    /// </summary>
    private static string ManifestBrowserLabel() =>
        BrowserKindParser.TryParse(Environment.GetEnvironmentVariable("AUTOMATION_BROWSER"), out BrowserKind kind)
            ? kind.ToString().ToLowerInvariant()
            : "not-applicable";

    private static int TestContextWorkers() =>
        int.TryParse(Environment.GetEnvironmentVariable("AUTOMATION_WORKERS"), out int workers) ? workers : 0;

    private static BrowserKind ResolveBrowser(BrowserOptions options)
    {
        string? requested = Environment.GetEnvironmentVariable("AUTOMATION_BROWSER");
        return BrowserKindParser.TryParse(requested, out BrowserKind kind) ? kind : options.DefaultBrowser;
    }
}
