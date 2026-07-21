using System.Text.Json;
using System.Text.Json.Serialization;
using Automation.Core.Artifacts;
using Automation.Core.Identity;

namespace Automation.Core.Diagnostics;

/// <summary>Stable machine-readable run summary (guide section 15.3). Never contains secrets.</summary>
public sealed record RunManifest
{
    public int SchemaVersion { get; init; } = 1;

    public required string RunId { get; init; }

    public string? Commit { get; init; }

    public string? Runner { get; init; }

    /// <summary>Selected browser, or <c>not-applicable</c> for a browser-free (API/Database) run.</summary>
    public string? Browser { get; init; }

    public string? Type { get; init; }

    public string? Suite { get; init; }

    /// <summary>
    /// How tests were selected: <c>category</c> (type/suite/tags) or <c>test-name</c> (a targeted
    /// <c>-TestName</c> run, where <see cref="Type"/>/<see cref="Suite"/> reflect the launcher
    /// defaults rather than the actual test's categories).
    /// </summary>
    public string SelectionMode { get; init; } = "category";

    /// <summary>The requested test-name fragment for a <c>test-name</c> selection; otherwise null.</summary>
    public string? TestName { get; init; }

    public int Workers { get; init; }

    public required DateTimeOffset StartedUtc { get; init; }

    public DateTimeOffset? CompletedUtc { get; init; }

    /// <summary>One of <c>passed</c>, <c>failed</c>, <c>incomplete</c>, or <c>unknown</c>.</summary>
    public string Result { get; init; } = "unknown";

    public RunManifestPaths Paths { get; init; } = new();
}

/// <summary>
/// Paths are relative to the run root (this manifest's own directory,
/// <c>artifacts/&lt;run-id&gt;</c>) with forward slashes, so they stay valid after the artifacts are
/// downloaded to another machine or opened in a different checkout.
/// </summary>
public sealed record RunManifestPaths
{
    /// <summary>Base the other paths are relative to.</summary>
    public string RelativeTo { get; init; } = "run-root";

    public string AllureResults { get; init; } = "../../allure-results";

    public string Trx { get; init; } = "test-results.trx";

    public string Tests { get; init; } = "tests";
}

/// <summary>Writes and reads <see cref="RunManifest"/> documents.</summary>
public static class RunManifestWriter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static RunManifest Build(RunContext run, ArtifactPaths paths, string? type, string? suite, string? browser, int workers, string result, DateTimeOffset? completedUtc = null, string selectionMode = "category", string? testName = null)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(paths);

        return new RunManifest
        {
            RunId = run.RunId,
            Commit = run.Commit,
            Runner = run.Runner,
            Browser = browser,
            Type = type,
            Suite = suite,
            SelectionMode = selectionMode,
            TestName = testName,
            Workers = workers,
            StartedUtc = run.StartedUtc,
            CompletedUtc = completedUtc,
            Result = result,
            Paths = new RunManifestPaths
            {
                RelativeTo = "run-root",
                AllureResults = RelativeToRunRoot(paths.RunRoot, paths.AllureResultsDirectory),
                Trx = RelativeToRunRoot(paths.RunRoot, paths.TrxPath),
                Tests = RelativeToRunRoot(paths.RunRoot, paths.TestsDirectory),
            },
        };
    }

    /// <summary>Returns <paramref name="target"/> relative to the run root, using forward slashes.</summary>
    private static string RelativeToRunRoot(string runRoot, string target) =>
        Path.GetRelativePath(runRoot, target).Replace('\\', '/');

    public static void Write(string path, RunManifest manifest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(manifest);

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(manifest, Options));
    }

    public static string Serialize(RunManifest manifest) => JsonSerializer.Serialize(manifest, Options);
}
