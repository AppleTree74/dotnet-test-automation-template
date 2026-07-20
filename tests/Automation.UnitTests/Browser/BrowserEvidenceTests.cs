using Automation.Browser.Evidence;
using Automation.Core.Configuration;
using Automation.Core.Redaction;
using NUnit.Framework;

namespace Automation.UnitTests.Browser;

[TestFixture]
public sealed class BrowserEvidenceTests
{
    private static Redactor CreateRedactor() => new(new RedactionOptions());

    [Test]
    public void SerializeConsole_RedactsSecretsInTextAndLocation()
    {
        Redactor redactor = CreateRedactor();
        ConsoleMessageRecord[] records =
        [
            new()
            {
                Timestamp = DateTimeOffset.UtcNow,
                Type = "error",
                Text = "request failed with Authorization: Bearer abc123.def-456_secret",
                Location = "https://app.test/callback?access_token=leaked-token-value&page=2",
            },
        ];

        string jsonl = BrowserEvidence.SerializeConsole(redactor, records);

        Assert.Multiple(() =>
        {
            Assert.That(jsonl, Does.Not.Contain("abc123.def-456_secret"));
            Assert.That(jsonl, Does.Not.Contain("leaked-token-value"));
            Assert.That(jsonl, Does.Contain("***REDACTED***"));
            Assert.That(jsonl, Does.Contain("page=2"));
        });
    }

    [Test]
    public void RedactHtml_MasksBearerTokenAndConnectionSecrets()
    {
        Redactor redactor = CreateRedactor();
        const string Html =
            "<html><body><script>var t='Bearer supersecrettoken1234567890';" +
            "var c='Server=db;User ID=svc;Password=P@ssw0rd!;';</script></body></html>";

        string result = BrowserEvidence.RedactHtml(redactor, Html);

        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Not.Contain("supersecrettoken1234567890"));
            Assert.That(result, Does.Not.Contain("P@ssw0rd!"));
            Assert.That(result, Does.Contain("<html>"));
        });
    }

    [Test]
    public void RedactHtml_BoundsLength()
    {
        Redactor redactor = CreateRedactor();
        string html = new('x', BrowserEvidence.MaxHtmlLength + 5000);

        string result = BrowserEvidence.RedactHtml(redactor, html);

        Assert.Multiple(() =>
        {
            Assert.That(result.Length, Is.LessThanOrEqualTo(BrowserEvidence.MaxHtmlLength + 64));
            Assert.That(result, Does.Contain("[truncated]"));
        });
    }

    [Test]
    public void RedactHtml_EmptyInput_ReturnsEmpty()
    {
        Assert.That(BrowserEvidence.RedactHtml(CreateRedactor(), null), Is.Empty);
    }
}
