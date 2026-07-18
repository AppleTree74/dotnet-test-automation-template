using System.Text.Json;
using Automation.Core.Artifacts;
using Automation.Core.Configuration;
using Automation.Core.Diagnostics;
using Automation.Core.Identity;
using NUnit.Framework;

namespace Automation.UnitTests.Core;

[TestFixture]
public sealed class IdentityAndManifestTests
{
    [Test]
    public void RunContext_UsesGithubRunIdWhenPresent()
    {
        RunContext run = RunContext.Create(new Dictionary<string, string?>
        {
            ["GITHUB_RUN_ID"] = "1234567",
            ["GITHUB_SHA"] = "abc",
        });

        Assert.Multiple(() =>
        {
            Assert.That(run.RunId, Does.EndWith("-1234567"));
            Assert.That(run.RunId, Does.Match(@"^\d{8}T\d{6}Z-1234567$"));
            Assert.That(run.Commit, Is.EqualTo("abc"));
        });
    }

    [Test]
    public void TestIdentity_RequiresAtLeastOneSuite()
    {
        Assert.Throws<ArgumentException>(() =>
            TestIdentity.Create("A.B.C", TestType.UI, Array.Empty<Suite>()));
    }

    [Test]
    public void TestIdentity_DefaultsBrowserToNotApplicable()
    {
        TestIdentity identity = TestIdentity.Create("A.B.C", TestType.API, [Suite.Smoke]);

        Assert.That(identity.Browser, Is.EqualTo(TestIdentity.NotApplicable));
    }

    [Test]
    public void RunManifest_SerializesSchemaVersionOne_WithCamelCase()
    {
        RunContext run = RunContext.Create(new Dictionary<string, string?> { ["GITHUB_RUN_ID"] = "42" });
        var paths = new ArtifactPaths(new ArtifactOptions { RootDirectory = "artifacts" }, run);
        RunManifest manifest = RunManifestWriter.Build(run, paths, "ui", "smoke", "chromium", 4, "passed");

        string json = RunManifestWriter.Serialize(manifest);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(1));
            Assert.That(root.GetProperty("runId").GetString(), Is.EqualTo(run.RunId));
            Assert.That(root.GetProperty("result").GetString(), Is.EqualTo("passed"));
            Assert.That(root.GetProperty("paths").GetProperty("tests").GetString(), Is.Not.Empty);
        });
    }
}
