using Automation.Core.Artifacts;
using Automation.Core.Configuration;
using Automation.Core.Exceptions;
using Automation.Core.Identity;
using NUnit.Framework;

namespace Automation.UnitTests.Core;

[TestFixture]
public sealed class ArtifactPathsTests
{
    private static ArtifactPaths CreatePaths(out string tempRoot)
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "automation-tests", Guid.NewGuid().ToString("N"));
        var options = new ArtifactOptions { RootDirectory = tempRoot };
        RunContext run = RunContext.Create(new Dictionary<string, string?>());
        return new ArtifactPaths(options, run);
    }

    [Test]
    public void RunRoot_IsUnderConfiguredRoot()
    {
        ArtifactPaths paths = CreatePaths(out string tempRoot);

        Assert.That(paths.RunRoot, Does.StartWith(Path.GetFullPath(tempRoot)));
    }

    [Test]
    public void TestDirectory_StaysInsideRunRoot()
    {
        ArtifactPaths paths = CreatePaths(out _);
        TestIdentity identity = TestIdentity.Create(
            "Application.Tests.UI.SampleTests.Example",
            TestType.UI,
            [Suite.Smoke],
            browser: "chromium");

        string dir = paths.TestDirectory(identity);

        Assert.That(dir, Does.StartWith(Path.GetFullPath(paths.TestsDirectory)));
    }

    [Test]
    public void TestDirectory_RejectsTraversalId()
    {
        ArtifactPaths paths = CreatePaths(out _);

        Assert.Throws<UnsafeArtifactPathException>(() => paths.TestDirectory("../../escape"));
    }

    [Test]
    public void TestArtifact_RejectsNestedFileName()
    {
        ArtifactPaths paths = CreatePaths(out _);
        TestIdentity identity = TestIdentity.Create(
            "Application.Tests.UI.SampleTests.Example",
            TestType.UI,
            [Suite.Smoke]);

        Assert.Throws<UnsafeArtifactPathException>(() => paths.TestArtifact(identity, "../evil.png"));
    }

    [Test]
    public void EnsureTestDirectory_CreatesDirectory()
    {
        ArtifactPaths paths = CreatePaths(out _);
        TestIdentity identity = TestIdentity.Create(
            "Application.Tests.API.SampleTests.Example",
            TestType.API,
            [Suite.Smoke, Suite.Regression]);

        string dir = paths.EnsureTestDirectory(identity);

        Assert.That(Directory.Exists(dir), Is.True);
        Directory.Delete(paths.RunRoot, recursive: true);
    }
}
