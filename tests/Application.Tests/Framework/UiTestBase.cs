using Automation.Browser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Application.Tests.Framework;

/// <summary>
/// Base for UI tests. Provides a fresh, isolated <see cref="BrowserSession"/> and page per test
/// and captures full browser evidence on failure before the context is disposed.
/// </summary>
public abstract class UiTestBase : AutomationTestBase
{
    private BrowserSession _session = null!;

    /// <summary>The isolated page under test.</summary>
    protected IPage Page => _session.Page;

    protected BrowserSession Session => _session;

    [SetUp]
    public async Task UiSetUp()
    {
        var factory = Services.GetRequiredService<IBrowserSessionFactory>();
        _session = await factory.CreateAsync(TestRun.SelectedBrowser, TestArtifactDirectory);
    }

    [TearDown]
    public async Task UiTearDown()
    {
        // A failed base SetUp leaves no session to tear down.
        if (_session is null)
        {
            return;
        }

        // Runs before AutomationTearDown; capture evidence while the context is still alive.
        if (TestFailed)
        {
            await _session.CaptureFailureEvidenceAsync();
        }

        await _session.DisposeAsync();
    }
}
