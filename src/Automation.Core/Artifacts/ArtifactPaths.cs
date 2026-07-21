using Automation.Core.Configuration;
using Automation.Core.Exceptions;
using Automation.Core.Identity;

namespace Automation.Core.Artifacts;

/// <summary>
/// Resolves and creates the contractual artifact layout (guide section 7.2). Note that
/// <c>allure-results/</c> lives at the repository root, not under the run (see ADR 0001):
/// <code>
/// artifacts/&lt;run-id&gt;/
///   run-manifest.json
///   test-results.trx
///   tests/&lt;test-id&gt;/...
/// allure-results/
/// </code>
/// All per-test paths are verified to remain inside the run root, defeating traversal via a
/// malformed id.
/// </summary>
public sealed class ArtifactPaths
{
    private readonly ArtifactOptions _options;

    public ArtifactPaths(ArtifactOptions options, RunContext run, string? repositoryRoot = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        ArgumentNullException.ThrowIfNull(run);

        string repoRoot = repositoryRoot ?? Directory.GetCurrentDirectory();

        string root = Path.IsPathRooted(options.RootDirectory)
            ? options.RootDirectory
            : Path.Combine(repoRoot, options.RootDirectory);

        RunRoot = Path.GetFullPath(Path.Combine(root, run.RunId));

        // Allure's live results directory is top-level and cleaned per run (see ADR 0001).
        AllureResultsDirectory = Path.IsPathRooted(options.AllureResultsDirectoryName)
            ? options.AllureResultsDirectoryName
            : Path.GetFullPath(Path.Combine(repoRoot, options.AllureResultsDirectoryName));
    }

    /// <summary>Absolute path of <c>artifacts/&lt;run-id&gt;</c>.</summary>
    public string RunRoot { get; }

    public string RunManifestPath => Path.Combine(RunRoot, "run-manifest.json");

    /// <summary>Top-level Allure results directory (repository root), not nested under the run.</summary>
    public string AllureResultsDirectory { get; }

    public string TrxPath => Path.Combine(RunRoot, "test-results.trx");

    public string TestsDirectory => Path.Combine(RunRoot, "tests");

    public string EnsureRunDirectories()
    {
        Directory.CreateDirectory(RunRoot);
        Directory.CreateDirectory(AllureResultsDirectory);
        Directory.CreateDirectory(TestsDirectory);
        return RunRoot;
    }

    /// <summary>Absolute directory for one test's evidence, guaranteed to be inside the run root.</summary>
    public string TestDirectory(TestIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return TestDirectory(identity.TestId);
    }

    public string TestDirectory(string testId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testId);

        string candidate = Path.GetFullPath(Path.Combine(TestsDirectory, testId));
        string testsFull = Path.GetFullPath(TestsDirectory);
        if (!IsInside(testsFull, candidate))
        {
            throw new UnsafeArtifactPathException(
                $"Test id '{testId}' resolved outside the artifacts root and was rejected.");
        }

        return candidate;
    }

    public string EnsureTestDirectory(TestIdentity identity)
    {
        string dir = TestDirectory(identity);
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>Absolute path to a named evidence file inside a test's directory.</summary>
    public string TestArtifact(TestIdentity identity, string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains(".."))
        {
            throw new UnsafeArtifactPathException($"Artifact file name '{fileName}' is not a bare file name.");
        }

        return Path.Combine(TestDirectory(identity), fileName);
    }

    private static bool IsInside(string parentFull, string candidateFull)
    {
        string withSep = parentFull.EndsWith(Path.DirectorySeparatorChar)
            ? parentFull
            : parentFull + Path.DirectorySeparatorChar;

        return candidateFull.StartsWith(withSep, StringComparison.Ordinal)
            || string.Equals(candidateFull, parentFull, StringComparison.Ordinal);
    }
}
