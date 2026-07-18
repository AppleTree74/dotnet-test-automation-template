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

    /// <summary>Directory (under the run root) that Allure results are written to.</summary>
    [Required]
    public string AllureResultsDirectoryName { get; init; } = "allure-results";
}
