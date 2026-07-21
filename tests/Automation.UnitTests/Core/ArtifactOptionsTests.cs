using Automation.Core.Artifacts;
using Automation.Core.Configuration;
using NUnit.Framework;

namespace Automation.UnitTests.Core;

[TestFixture]
public sealed class ArtifactOptionsTests
{
    [Test]
    public void ShouldAttachToReport_DefaultsWithholdAllRawBinaryEvidence()
    {
        // Template default (P1-01/P1-1): no un-redactable binary is published. Screenshots, traces,
        // HAR, and video are all opt-in; only sanitized text evidence is attached by default.
        var options = new ArtifactOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.ShouldAttachToReport(ArtifactNames.Screenshot), Is.False);
            Assert.That(options.ShouldAttachToReport(ArtifactNames.Trace), Is.False);
            Assert.That(options.ShouldAttachToReport(ArtifactNames.Har), Is.False);
            Assert.That(options.ShouldAttachToReport(ArtifactNames.Video), Is.False);
        });
    }

    [Test]
    public void ShouldAttachToReport_AlwaysAttachesSanitizedTextEvidence()
    {
        // Sanitized text evidence is attached regardless of the binary policy.
        var options = new ArtifactOptions
        {
            AttachScreenshotToReport = false,
            AttachTraceToReport = false,
            AttachHarToReport = false,
            AttachVideoToReport = false,
        };

        Assert.Multiple(() =>
        {
            Assert.That(options.ShouldAttachToReport(ArtifactNames.PageHtml), Is.True);
            Assert.That(options.ShouldAttachToReport(ArtifactNames.BrowserConsole), Is.True);
            Assert.That(options.ShouldAttachToReport(ArtifactNames.ApiEvidence), Is.True);
            Assert.That(options.ShouldAttachToReport(ArtifactNames.SqlEvidence), Is.True);
            Assert.That(options.ShouldAttachToReport(ArtifactNames.TestLog), Is.True);
            Assert.That(options.ShouldAttachToReport("current-url.txt"), Is.True);
        });
    }

    [Test]
    public void ShouldAttachToReport_EnabledFlags_AttachTheMatchingBinary()
    {
        var options = new ArtifactOptions
        {
            AttachScreenshotToReport = true,
            AttachTraceToReport = true,
            AttachHarToReport = true,
            AttachVideoToReport = true,
        };

        Assert.Multiple(() =>
        {
            Assert.That(options.ShouldAttachToReport(ArtifactNames.Screenshot), Is.True);
            Assert.That(options.ShouldAttachToReport(ArtifactNames.Trace), Is.True);
            Assert.That(options.ShouldAttachToReport(ArtifactNames.Har), Is.True);
            Assert.That(options.ShouldAttachToReport(ArtifactNames.Video), Is.True);
        });
    }

    [Test]
    public void ShouldAttachToReport_VideoDecisionIsByExtension_NotCanonicalName()
    {
        // Playwright names videos itself (page@<id>.webm); the policy must gate ANY .webm by the
        // video flag, not only the canonical video.webm, so exclusion cannot fail open (P1-2).
        var withheld = new ArtifactOptions { AttachVideoToReport = false };
        var allowed = new ArtifactOptions { AttachVideoToReport = true };

        Assert.Multiple(() =>
        {
            Assert.That(withheld.ShouldAttachToReport("page@1a2b3c4d.webm"), Is.False);
            Assert.That(withheld.ShouldAttachToReport("VIDEO.WEBM"), Is.False, "extension match is case-insensitive");
            Assert.That(allowed.ShouldAttachToReport("page@1a2b3c4d.webm"), Is.True);
        });
    }
}
