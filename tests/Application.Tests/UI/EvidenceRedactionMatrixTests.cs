using Allure.NUnit;
using Allure.NUnit.Attributes;
using Application.Tests.Framework;
using Automation.Browser;
using Automation.Core.Artifacts;
using Automation.Core.Configuration;
using Automation.Core.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Application.Tests.UI;

/// <summary>
/// Security self-test for the evidence-publication boundary. It plants distinct fake secrets in the
/// page URL, the browser console, and the DOM, runs the real <see cref="BrowserSession"/>
/// failure-evidence pipeline, then asserts on every captured output:
/// <list type="bullet">
///   <item><description><b>Redacted (must exclude the secret):</b> current URL, browser console, and page HTML — the sanitized text evidence that is always attached to the report.</description></item>
///   <item><description><b>Captured but un-redactable (withheld from the report by default):</b> screenshot, trace, HAR, and video. These cannot be centrally redacted, so the attachment policy — by artifact type — withholds them all from Pages under template defaults (P1-1), while raw copies remain in the restricted CI workflow artifacts.</description></item>
/// </list>
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

        // Binary evidence is captured on disk (available through workflow artifacts)...
        string screenshotPath = Path.Combine(TestArtifactDirectory, ArtifactNames.Screenshot);
        string tracePath = Path.Combine(TestArtifactDirectory, ArtifactNames.Trace);
        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(screenshotPath), Is.True, "Screenshot was not captured.");
            Assert.That(File.Exists(tracePath), Is.True, "Trace was not captured for workflow artifacts.");
        });

        // ...but under template defaults the attachment policy withholds every un-redactable binary
        // from the report — screenshot included (P1-1) — while attaching all sanitized text. The
        // video decision is by extension, so a Playwright-named .webm cannot slip through (P1-2).
        var artifacts = Services.GetRequiredService<ArtifactOptions>();
        Assert.Multiple(() =>
        {
            Assert.That(artifacts.ShouldAttachToReport(ArtifactNames.Screenshot), Is.False, "Screenshot must not be published by default.");
            Assert.That(artifacts.ShouldAttachToReport(ArtifactNames.Trace), Is.False);
            Assert.That(artifacts.ShouldAttachToReport(ArtifactNames.Har), Is.False);
            Assert.That(artifacts.ShouldAttachToReport("page@deadbeef.webm"), Is.False, "Playwright-named video must be withheld.");
            Assert.That(artifacts.ShouldAttachToReport(CurrentUrlFile), Is.True);
            Assert.That(artifacts.ShouldAttachToReport(ArtifactNames.PageHtml), Is.True);
            Assert.That(artifacts.ShouldAttachToReport(ArtifactNames.BrowserConsole), Is.True);
        });
    }

    private async Task<string> ReadEvidenceAsync(string fileName)
    {
        string path = Path.Combine(TestArtifactDirectory, fileName);
        Assert.That(File.Exists(path), Is.True, $"Expected evidence file '{fileName}' was not written.");
        return await File.ReadAllTextAsync(path);
    }
}
