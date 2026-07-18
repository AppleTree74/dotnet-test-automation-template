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

    public string? Browser { get; init; }

    public string? Type { get; init; }

    public string? Suite { get; init; }

    public int Workers { get; init; }

    public required DateTimeOffset StartedUtc { get; init; }

    public DateTimeOffset? CompletedUtc { get; init; }

    /// <summary>One of <c>passed</c>, <c>failed</c>, <c>incomplete</c>, or <c>unknown</c>.</summary>
    public string Result { get; init; } = "unknown";

    public RunManifestPaths Paths { get; init; } = new();
}

public sealed record RunManifestPaths
{
    public string AllureResults { get; init; } = "allure-results";

    public string Trx { get; init; } = string.Empty;

    public string Tests { get; init; } = string.Empty;
}

/// <summary>Writes and reads <see cref="RunManifest"/> documents.</summary>
public static class RunManifestWriter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static RunManifest Build(RunContext run, ArtifactPaths paths, string? type, string? suite, string? browser, int workers, string result, DateTimeOffset? completedUtc = null)
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
            Workers = workers,
            StartedUtc = run.StartedUtc,
            CompletedUtc = completedUtc,
            Result = result,
            Paths = new RunManifestPaths
            {
                AllureResults = paths.AllureResultsDirectory,
                Trx = paths.TrxPath,
                Tests = paths.TestsDirectory,
            },
        };
    }

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
