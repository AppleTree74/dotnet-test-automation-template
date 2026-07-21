using System.ComponentModel.DataAnnotations;
using Automation.Core.Artifacts;

namespace Automation.Core.Configuration;

/// <summary>
/// Controls where per-run and per-test evidence is written. The layout is contractual
/// (guide section 7.2): <c>artifacts/&lt;run-id&gt;/tests/&lt;test-id&gt;/...</c>.
/// </summary>
public sealed class ArtifactOptions
{
    public const string SectionName = "Artifacts";

    /// <summary>Root directory for all artifacts, relative to the repository root or absolute.</summary>
    [Required]
    public string RootDirectory { get; init; } = "artifacts";

    /// <summary>
    /// Name of the top-level Allure results directory (at the repository root, not nested under the
    /// run), cleaned before each run. See ADR 0001.
    /// </summary>
    [Required]
    public string AllureResultsDirectoryName { get; init; } = "allure-results";

    /// <summary>
    /// Whether the failure screenshot is attached to the Allure report (and thus published to
    /// GitHub Pages). On by default: a screenshot is the most useful at-a-glance diagnostic and only
    /// exposes what was on screen. It cannot be centrally redacted, so turn this off for
    /// applications that render sensitive data. See P1-01 and <c>docs/configuration.md</c>.
    /// </summary>
    public bool AttachScreenshotToReport { get; init; } = true;

    /// <summary>
    /// Whether the Playwright trace (<c>trace.zip</c>) is attached to the Allure report. Off by
    /// default: a trace contains full DOM snapshots, page sources, and network bodies that cannot be
    /// redacted after capture. It is always captured and remains available in the restricted CI
    /// workflow artifacts for time-travel debugging regardless of this flag.
    /// </summary>
    public bool AttachTraceToReport { get; init; }

    /// <summary>Whether the HTTP archive (<c>network.har</c>) is attached to the Allure report. Off by default; raw HAR stays in workflow artifacts.</summary>
    public bool AttachHarToReport { get; init; }

    /// <summary>Whether the session video is attached to the Allure report. Off by default; raw video stays in workflow artifacts.</summary>
    public bool AttachVideoToReport { get; init; }

    /// <summary>
    /// Raw binary evidence file names that must NOT be attached to the Allure report under the
    /// current policy. Sanitized text evidence (URL, console, page HTML, API/SQL evidence, logs) is
    /// always attached; these binary files cannot be centrally redacted, so publishing them to Pages
    /// is opt-in. Every captured file always remains in the restricted CI workflow artifacts.
    /// </summary>
    public IReadOnlyCollection<string> ReportExcludedFileNames()
    {
        var excluded = new List<string>();
        if (!AttachScreenshotToReport) { excluded.Add(ArtifactNames.Screenshot); }
        if (!AttachTraceToReport) { excluded.Add(ArtifactNames.Trace); }
        if (!AttachHarToReport) { excluded.Add(ArtifactNames.Har); }
        if (!AttachVideoToReport) { excluded.Add(ArtifactNames.Video); }
        return excluded;
    }
}
