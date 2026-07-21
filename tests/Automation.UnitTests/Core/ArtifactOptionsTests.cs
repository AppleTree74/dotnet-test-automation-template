using Automation.Core.Artifacts;
using Automation.Core.Configuration;
using NUnit.Framework;

namespace Automation.UnitTests.Core;

[TestFixture]
public sealed class ArtifactOptionsTests
{
    [Test]
    public void ReportExcludedFileNames_DefaultsAttachScreenshotButWithholdTraceHarVideo()
    {
        // The template default (P1-01): screenshots are published to the report; the trace, HAR, and
        // video are withheld because they cannot be centrally redacted.
        var options = new ArtifactOptions();

        IReadOnlyCollection<string> excluded = options.ReportExcludedFileNames();

        Assert.Multiple(() =>
        {
            Assert.That(excluded, Does.Not.Contain(ArtifactNames.Screenshot));
            Assert.That(excluded, Does.Contain(ArtifactNames.Trace));
            Assert.That(excluded, Does.Contain(ArtifactNames.Har));
            Assert.That(excluded, Does.Contain(ArtifactNames.Video));
        });
    }

    [Test]
    public void ReportExcludedFileNames_AllAttachmentsEnabled_ExcludesNothing()
    {
        var options = new ArtifactOptions
        {
            AttachScreenshotToReport = true,
            AttachTraceToReport = true,
            AttachHarToReport = true,
            AttachVideoToReport = true,
        };

        Assert.That(options.ReportExcludedFileNames(), Is.Empty);
    }

    [Test]
    public void ReportExcludedFileNames_FullyConservative_ExcludesEveryBinaryEvidenceFile()
    {
        var options = new ArtifactOptions
        {
            AttachScreenshotToReport = false,
            AttachTraceToReport = false,
            AttachHarToReport = false,
            AttachVideoToReport = false,
        };

        Assert.That(
            options.ReportExcludedFileNames(),
            Is.EquivalentTo(new[]
            {
                ArtifactNames.Screenshot,
                ArtifactNames.Trace,
                ArtifactNames.Har,
                ArtifactNames.Video,
            }));
    }

    [Test]
    public void ReportExcludedFileNames_NeverWithholdsSanitizedTextEvidence()
    {
        // Sanitized text evidence is always attached regardless of the binary policy.
        var options = new ArtifactOptions
        {
            AttachScreenshotToReport = false,
            AttachTraceToReport = false,
            AttachHarToReport = false,
            AttachVideoToReport = false,
        };

        IReadOnlyCollection<string> excluded = options.ReportExcludedFileNames();

        Assert.Multiple(() =>
        {
            Assert.That(excluded, Does.Not.Contain(ArtifactNames.PageHtml));
            Assert.That(excluded, Does.Not.Contain(ArtifactNames.BrowserConsole));
            Assert.That(excluded, Does.Not.Contain(ArtifactNames.ApiEvidence));
            Assert.That(excluded, Does.Not.Contain(ArtifactNames.SqlEvidence));
            Assert.That(excluded, Does.Not.Contain(ArtifactNames.TestLog));
        });
    }
}
