using System.Globalization;

namespace Automation.Core.Identity;

/// <summary>
/// Identity shared by every test in a single run. A <see cref="RunId"/> is stable for the
/// life of the process and appears in the run manifest and all artifact paths.
/// </summary>
public sealed class RunContext
{
    private RunContext(string runId, DateTimeOffset startedUtc, string? commit, string? runner, string? githubRunId)
    {
        RunId = runId;
        StartedUtc = startedUtc;
        Commit = commit;
        Runner = runner;
        GithubRunId = githubRunId;
    }

    public string RunId { get; }

    public DateTimeOffset StartedUtc { get; }

    public string? Commit { get; }

    public string? Runner { get; }

    public string? GithubRunId { get; }

    /// <summary>
    /// Creates a run context. The id is <c>yyyyMMddTHHmmssZ-&lt;suffix&gt;</c>, where the
    /// suffix prefers the GitHub run id and otherwise uses a random 7-digit number so that
    /// concurrent local runs never collide.
    /// </summary>
    public static RunContext Create(IReadOnlyDictionary<string, string?>? environment = null)
    {
        environment ??= CaptureEnvironment();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // A launcher (scripts/Invoke-Tests.ps1) may fix the run id so it and the test host agree
        // on the artifact directory. Honour it when present.
        string? explicitRunId = Value(environment, "AUTOMATION_RUN_ID");
        if (!string.IsNullOrWhiteSpace(explicitRunId))
        {
            return new RunContext(
                explicitRunId,
                now,
                Value(environment, "GITHUB_SHA"),
                Value(environment, "RUNNER_OS") ?? Value(environment, "AUTOMATION_RUNNER"),
                Value(environment, "GITHUB_RUN_ID"));
        }

        string? githubRunId = Value(environment, "GITHUB_RUN_ID");
        string suffix = !string.IsNullOrWhiteSpace(githubRunId)
            ? new string(githubRunId.Where(char.IsAsciiLetterOrDigit).ToArray())
            : Random.Shared.Next(1_000_000, 9_999_999).ToString(CultureInfo.InvariantCulture);

        string runId = string.Create(CultureInfo.InvariantCulture, $"{now:yyyyMMddTHHmmss}Z-{suffix}");

        return new RunContext(
            runId,
            now,
            Value(environment, "GITHUB_SHA"),
            Value(environment, "RUNNER_OS") ?? Value(environment, "AUTOMATION_RUNNER"),
            githubRunId);
    }

    private static Dictionary<string, string?> CaptureEnvironment()
    {
        string[] keys = ["AUTOMATION_RUN_ID", "GITHUB_RUN_ID", "GITHUB_SHA", "RUNNER_OS", "AUTOMATION_RUNNER"];
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (string key in keys)
        {
            map[key] = Environment.GetEnvironmentVariable(key);
        }

        return map;
    }

    private static string? Value(IReadOnlyDictionary<string, string?> environment, string key) =>
        environment.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value) ? value : null;
}
