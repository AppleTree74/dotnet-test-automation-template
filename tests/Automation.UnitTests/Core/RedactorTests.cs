using Automation.Core.Configuration;
using Automation.Core.Redaction;
using NUnit.Framework;

namespace Automation.UnitTests.Core;

[TestFixture]
public sealed class RedactorTests
{
    private static Redactor CreateRedactor() => new(new RedactionOptions());

    [Test]
    public void RedactText_MasksBearerToken()
    {
        Redactor redactor = CreateRedactor();

        string result = redactor.RedactText("Authorization: Bearer abc123.def-456_GHI");

        Assert.That(result, Does.Not.Contain("abc123"));
        Assert.That(result, Does.Contain("***REDACTED***"));
    }

    [Test]
    public void RedactHeaderValue_MasksSensitiveHeaders()
    {
        Redactor redactor = CreateRedactor();

        Assert.Multiple(() =>
        {
            Assert.That(redactor.RedactHeaderValue("Authorization", "Bearer secret"), Is.EqualTo("***REDACTED***"));
            Assert.That(redactor.RedactHeaderValue("Cookie", "session=abc"), Is.EqualTo("***REDACTED***"));
            Assert.That(redactor.RedactHeaderValue("Accept", "application/json"), Is.EqualTo("application/json"));
        });
    }

    [Test]
    public void RedactJson_MasksSecretNamedFields()
    {
        Redactor redactor = CreateRedactor();
        const string Json = """{"user":"alice","password":"hunter2","nested":{"api_key":"xyz"},"keep":42}""";

        string result = redactor.RedactJson(Json);

        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Not.Contain("hunter2"));
            Assert.That(result, Does.Not.Contain("xyz"));
            Assert.That(result, Does.Contain("alice"));
            Assert.That(result, Does.Contain("42"));
        });
    }

    [Test]
    public void RedactJson_FallsBackToTextRedaction_ForInvalidJson()
    {
        Redactor redactor = CreateRedactor();

        string result = redactor.RedactJson("not json but token=supersecretvalue trailing");

        Assert.That(result, Does.Not.Contain("supersecretvalue"));
    }

    [Test]
    public void RedactConnectionString_MasksPasswordAndUser()
    {
        Redactor redactor = CreateRedactor();
        const string ConnectionString = "Server=db;Database=app;User ID=svc;Password=P@ssw0rd!;Encrypt=True";

        string result = redactor.RedactConnectionString(ConnectionString);

        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Not.Contain("P@ssw0rd!"));
            Assert.That(result, Does.Not.Contain("svc"));
            Assert.That(result, Does.Contain("Server=db"));
            Assert.That(result, Does.Contain("Encrypt=True"));
        });
    }

    [Test]
    public void RedactUrl_MasksSecretQueryParametersOnly()
    {
        Redactor redactor = CreateRedactor();

        string result = redactor.RedactUrl("https://api.test/resource?id=5&access_token=leak&page=2");

        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Not.Contain("leak"));
            Assert.That(result, Does.Contain("id=5"));
            Assert.That(result, Does.Contain("page=2"));
        });
    }
}
