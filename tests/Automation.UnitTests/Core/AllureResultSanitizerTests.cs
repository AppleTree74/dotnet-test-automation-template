using System.Text.Json;
using System.Text.Json.Nodes;
using Automation.Core.Configuration;
using Automation.Core.Redaction;
using Automation.Core.Reporting;
using NUnit.Framework;

namespace Automation.UnitTests.Core;

[TestFixture]
public sealed class AllureResultSanitizerTests
{
    private static Redactor NewRedactor() => new(new RedactionOptions());

    private const string Mask = "***REDACTED***";

    [Test]
    public void SanitizeJson_MasksSecretsInStatusDetails_TheChannelAttachmentFiltersMiss()
    {
        // A Playwright assertion failure quotes the actual DOM/ARIA into statusDetails — the leak
        // path that is independent of attachment filtering (P1-3).
        string json = """
        {
          "name": "Login_ShowsToken",
          "status": "failed",
          "statusDetails": {
            "message": "Expected text but page rendered Authorization: Bearer SUPERSECRETTOKEN9999",
            "trace": "Server=db;User ID=svc;Password=P@ssw0rdLEAK; at LoginPage"
          }
        }
        """;

        JsonNode result = JsonNode.Parse(AllureResultSanitizer.SanitizeJson(json, NewRedactor()))!;

        Assert.Multiple(() =>
        {
            string message = result["statusDetails"]!["message"]!.GetValue<string>();
            string trace = result["statusDetails"]!["trace"]!.GetValue<string>();
            Assert.That(message, Does.Not.Contain("SUPERSECRETTOKEN9999"));
            Assert.That(message, Does.Contain($"Bearer {Mask}"));
            Assert.That(trace, Does.Not.Contain("P@ssw0rdLEAK"));
            Assert.That(trace, Does.Contain($"Password={Mask}"));
            // Non-secret content and structure are preserved.
            Assert.That(result["name"]!.GetValue<string>(), Is.EqualTo("Login_ShowsToken"));
            Assert.That(result["status"]!.GetValue<string>(), Is.EqualTo("failed"));
        });
    }

    [Test]
    public void SanitizeJson_MasksSecretsInParametersLabelsAndSteps()
    {
        string json = """
        {
          "name": "sample",
          "parameters": [ { "name": "auth", "value": "token=PARAMSECRET1234" } ],
          "labels": [ { "name": "tag", "value": "UI" } ],
          "steps": [ { "name": "Send Bearer HEADERSECRET5678", "status": "passed" } ]
        }
        """;

        JsonNode result = JsonNode.Parse(AllureResultSanitizer.SanitizeJson(json, NewRedactor()))!;

        Assert.Multiple(() =>
        {
            Assert.That(result["parameters"]![0]!["value"]!.GetValue<string>(), Does.Not.Contain("PARAMSECRET1234"));
            Assert.That(result["parameters"]![0]!["value"]!.GetValue<string>(), Is.EqualTo($"token={Mask}"));
            Assert.That(result["steps"]![0]!["name"]!.GetValue<string>(), Does.Not.Contain("HEADERSECRET5678"));
            // Ordinary label values pass through unchanged.
            Assert.That(result["labels"]![0]!["value"]!.GetValue<string>(), Is.EqualTo("UI"));
        });
    }

    [Test]
    public void SanitizeJson_PreservesNonStringValues()
    {
        string json = """{ "a": 1, "b": true, "c": null, "d": 3.5, "e": "plain" }""";

        JsonNode result = JsonNode.Parse(AllureResultSanitizer.SanitizeJson(json, NewRedactor()))!;

        Assert.Multiple(() =>
        {
            Assert.That(result["a"]!.GetValue<int>(), Is.EqualTo(1));
            Assert.That(result["b"]!.GetValue<bool>(), Is.True);
            Assert.That(result["c"], Is.Null);
            Assert.That(result["d"]!.GetValue<double>(), Is.EqualTo(3.5));
            Assert.That(result["e"]!.GetValue<string>(), Is.EqualTo("plain"));
        });
    }

    [Test]
    public void SanitizeJson_ThrowsOnInvalidJson_FailingClosed()
    {
        Assert.Catch<JsonException>(() => AllureResultSanitizer.SanitizeJson("{ not valid", NewRedactor()));
    }

    [Test]
    public void SanitizeDirectory_SanitizesJson_RedactsTextEvidence_AndCopiesBinary()
    {
        string input = Path.Combine(Path.GetTempPath(), "allure-san-in-" + Guid.NewGuid().ToString("N"));
        string output = Path.Combine(Path.GetTempPath(), "allure-san-out-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(input);
        try
        {
            File.WriteAllText(Path.Combine(input, "a-result.json"),
                """{ "statusDetails": { "message": "Bearer JSONSECRET0001" } }""");
            File.WriteAllText(Path.Combine(input, "b-attachment.txt"), "token=TEXTSECRET0002");
            byte[] binary = [0x00, 0x01, 0x02, 0xFF];
            File.WriteAllBytes(Path.Combine(input, "c-attachment.png"), binary);

            AllureResultSanitizer.SanitizeDirectory(input, output, NewRedactor());

            string resultJson = File.ReadAllText(Path.Combine(output, "a-result.json"));
            string textAttachment = File.ReadAllText(Path.Combine(output, "b-attachment.txt"));
            byte[] copiedBinary = File.ReadAllBytes(Path.Combine(output, "c-attachment.png"));

            Assert.Multiple(() =>
            {
                Assert.That(resultJson, Does.Not.Contain("JSONSECRET0001"));
                Assert.That(textAttachment, Is.EqualTo($"token={Mask}"));
                Assert.That(copiedBinary, Is.EqualTo(binary), "Binary attachments must be copied verbatim.");
            });
        }
        finally
        {
            Directory.Delete(input, recursive: true);
            if (Directory.Exists(output))
            {
                Directory.Delete(output, recursive: true);
            }
        }
    }
}
