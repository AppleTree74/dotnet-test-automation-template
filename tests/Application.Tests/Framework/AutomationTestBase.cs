using Automation.Core.Artifacts;
using Automation.Core.Identity;
using Automation.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Application.Tests.Framework;

/// <summary>
/// Base for every test. Resolves and validates the test's type/suite, allocates a unique per-test
/// artifact directory (safe under parallel execution), opens a per-test logging scope, and records
/// the outcome. Derived bases add channel-specific capability and failure evidence.
/// Assertions live in the concrete test, never in this base or in framework mechanics.
/// </summary>
public abstract class AutomationTestBase
{
    private IDisposable? _logScope;

    protected IServiceProvider Services => TestRun.Services;

    protected ILogger Logger { get; private set; } = null!;

    protected TestIdentity Identity { get; private set; } = null!;

    /// <summary>This test's private artifact directory: <c>artifacts/&lt;run-id&gt;/tests/&lt;test-id&gt;</c>.</summary>
    protected string TestArtifactDirectory { get; private set; } = null!;

    protected bool TestFailed =>
        TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed;

    [SetUp]
    public void AutomationSetUp()
    {
        Test test = TestExecutionContext.CurrentContext.CurrentTest;
        (TestType type, IReadOnlyList<Suite> suites) = CategoryConventions.ResolveAndValidate(test);

        bool usesBrowser = type is TestType.UI or TestType.E2E;
        string? browser = usesBrowser ? TestRun.SelectedBrowser.ToString().ToLowerInvariant() : null;

        Identity = TestIdentity.Create(
            test.FullName,
            type,
            suites,
            browser,
            worker: ParseWorker(TestContext.CurrentContext.WorkerId));

        TestArtifactDirectory = TestRun.Paths.EnsureTestDirectory(Identity);

        Logger = Services.GetRequiredService<ILoggerFactory>().CreateLogger(test.Name);
        _logScope = AutomationLogging.BeginTestScope(Logger, TestRun.Run, Identity, TestArtifactDirectory);

        Logger.LogInformation("Starting {Test} ({Type}) as {TestId}.", test.FullName, type, Identity.TestId);
    }

    [TearDown]
    public void AutomationTearDown()
    {
        // Runs after any derived [TearDown], which capture channel evidence first.
        bool passed = TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Passed;
        TestRun.RecordOutcome(passed);

        // Guard against a failed SetUp, where identity/logging were never established.
        if (Identity is null)
        {
            _logScope?.Dispose();
            _logScope = null;
            return;
        }

        Logger.LogInformation(
            "Finished {Test}: {Status}.",
            Identity.FullyQualifiedName,
            TestContext.CurrentContext.Result.Outcome.Status);

        AllureEvidence.AttachDirectory(TestArtifactDirectory);

        _logScope?.Dispose();
        _logScope = null;
    }

    private static int ParseWorker(string? workerId)
    {
        if (string.IsNullOrEmpty(workerId))
        {
            return 0;
        }

        string digits = new(workerId.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out int worker) ? worker : 0;
    }
}
