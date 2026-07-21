using System.ComponentModel.DataAnnotations;

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
    /// GitHub Pages). Off by default: image pixels cannot be redacted, and a page can render tokens,
    /// credentials, or personal data. The screenshot is always captured to the test artifact
    /// directory and remains in the restricted CI workflow artifacts; set this true only after
    /// confirming Pages access control and data classification. See P1-01/P1-1 and
    /// <c>docs/configuration.md</c>.
    /// </summary>
    public bool AttachScreenshotToReport { get; init; }

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
    /// Decides whether one captured evidence file may be attached to the Allure report (and thus
    /// published to GitHub Pages). The decision is by <b>artifact type</b>, keyed on the file
    /// extension, not an exact file name — Playwright owns the actual video file name
    /// (<c>page@&lt;id&gt;.webm</c>, not the canonical <c>video.webm</c>), so an exact-name check
    /// would fail open (P1-2). Raw binary evidence cannot be centrally redacted, so each kind is
    /// gated by its flag; sanitized text evidence (URL, console, page HTML, API/SQL evidence, logs)
    /// is always attached. Every captured file always remains in the restricted CI workflow
    /// artifacts regardless of this decision.
    /// </summary>
    public bool ShouldAttachToReport(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => AttachScreenshotToReport,
            ".zip" => AttachTraceToReport,
            ".har" => AttachHarToReport,
            ".webm" => AttachVideoToReport,
            _ => true,
        };
    }
}
