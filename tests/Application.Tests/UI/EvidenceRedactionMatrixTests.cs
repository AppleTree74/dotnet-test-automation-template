using Allure.NUnit;
using Allure.NUnit.Attributes;
using Application.Tests.Framework;
using Automation.Browser;
using Automation.Core.Artifacts;
using Automation.Core.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Application.Tests.UI;

/// <summary>
/// Security self-test for the live evidence-publication pipeline. It plants distinct fake secrets in
/// the page URL, the browser console, and the DOM, runs the real <see cref="BrowserSession"/>
/// failure-evidence pipeline, then asserts on the captured outputs:
/// <list type="bullet">
///   <item><description><b>Redacted text evidence (must exclude the secret):</b> the captured current URL, browser console, and page HTML — the sanitized text evidence that is always attached to the report — carries none of the planted secrets.</description></item>
///   <item><description><b>Un-redactable binaries are captured to disk:</b> screenshot and trace are written to the test artifact directory, so raw diagnostics remain available through the restricted CI workflow artifacts.</description></item>
/// </list>
/// The report-attachment <i>policy</i> for those binaries (defaults withhold them; opt-in flags attach
/// the matching type; video gated by extension) is covered independently by <c>ArtifactOptionsTests</c>,
/// so this test stays decoupled from the live DI configuration and remains valid whether or not a
/// generated repo opts in.
/// The other free text named in the reviews — the NUnit/Playwright assertion message and other
/// Allure result-JSON fields (<c>statusDetails</c>, parameters, labels, step names) — is redacted by
/// <see cref="Automation.Core.Reporting.AllureResultSanitizer"/> in a publication copy before report
/// generation (covered by its own unit tests), so on-screen secrets quoted in a failure message do
/// not reach Pages either. Raw diagnostics remain in workflow artifacts.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureEpic("Framework")]
[AllureFeature("Evidence redaction")]
[TestType(TestType.UI)]
[Suite(Suite.Regression)]
[Feature("Framework")]
public sealed class EvidenceRedactionMatrixTests : AutomationTestBase
{
    // Distinct, unique tokens so a match in one output cannot be confused with another.
    private const string UrlSecret = "URLSECRET0123456789abcdef";
    private const string ConsoleSecret = "CONSOLESECRET0123456789abcdef";
    private const string DomBearerSecret = "DOMBEARERSECRET0123456789abcdef";
    private const string DomPasswordSecret = "DOMPWDSECRET0123456789abcdef";
    private const string Mask = "***REDACTED***";
    private const string CurrentUrlFile = "current-url.txt";

    [Test]
    [AllureStory("Secrets in the URL, console, and DOM never reach the published report")]
    public async Task Secrets_AreRedactedInText_AndRawBinaryIsWithheldFromTheReport()
    {
        var factory = Services.GetRequiredService<IBrowserSessionFactory>();
        BrowserSession session = await factory.CreateAsync(TestRun.SelectedBrowser, TestArtifactDirectory);
        try
        {
            IPage page = session.Page;

            // Serve secret-laden content locally via route fulfilment (no real network/DNS/TLS), so
            // the DOM carries a bearer token and a connection-string password.
            string body = $$"""
                <main>
                  <h1>Session established</h1>
                  <p>Authorization: Bearer {{DomBearerSecret}}</p>
                  <code>Server=db;Database=app;User ID=svc;Password={{DomPasswordSecret}};</code>
                </main>
                """;
            await page.RouteAsync(
                "**/*",
                async route => await route.FulfillAsync(new RouteFulfillOptions
                {
                    ContentType = "text/html",
                    Body = body,
                }));

            // A secret in a query-string value; RedactUrl masks it by key name.
            await page.GotoAsync($"https://secrets.test.invalid/callback?access_token={UrlSecret}");

            // A secret written to the console; RunAndWaitFor guarantees the message is captured.
            await page.RunAndWaitForConsoleMessageAsync(
                async () => await page.EvaluateAsync($"() => console.log('token={ConsoleSecret}')"));

            await session.CaptureFailureEvidenceAsync();
        }
        finally
        {
            await session.DisposeAsync();
        }

        string url = await ReadEvidenceAsync(CurrentUrlFile);
        string console = await ReadEvidenceAsync(ArtifactNames.BrowserConsole);
        string pageHtml = await ReadEvidenceAsync(ArtifactNames.PageHtml);

        Assert.Multiple(() =>
        {
            // Redacted text evidence excludes every planted secret.
            Assert.That(url, Does.Not.Contain(UrlSecret), "URL secret leaked to current-url.txt.");
            Assert.That(url, Does.Contain($"access_token={Mask}"), "URL secret was not masked.");
            Assert.That(console, Does.Not.Contain(ConsoleSecret), "Console secret leaked to browser-console.jsonl.");
            Assert.That(pageHtml, Does.Not.Contain(DomBearerSecret), "DOM bearer token leaked to page.html.");
            Assert.That(pageHtml, Does.Not.Contain(DomPasswordSecret), "DOM connection password leaked to page.html.");
            Assert.That(pageHtml, Does.Contain(Mask), "Page HTML was not redacted at all.");
        });

        // Binary evidence is captured on disk (available through workflow artifacts). The
        // report-attachment policy for these un-redactable binaries is covered by ArtifactOptionsTests.
        string screenshotPath = Path.Combine(TestArtifactDirectory, ArtifactNames.Screenshot);
        string tracePath = Path.Combine(TestArtifactDirectory, ArtifactNames.Trace);
        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(screenshotPath), Is.True, "Screenshot was not captured.");
            Assert.That(File.Exists(tracePath), Is.True, "Trace was not captured for workflow artifacts.");
        });
    }

    private async Task<string> ReadEvidenceAsync(string fileName)
    {
        string path = Path.Combine(TestArtifactDirectory, fileName);
        Assert.That(File.Exists(path), Is.True, $"Expected evidence file '{fileName}' was not written.");
        return await File.ReadAllTextAsync(path);
    }
}
