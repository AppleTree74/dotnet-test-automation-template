using System.Text.Json;
using Automation.Core.Artifacts;

namespace Automation.SqlServer;

/// <summary>
/// Safe query evidence: only the query id, elapsed time, row count, and parameter names — never
/// parameter values or SQL text that could carry data (guide section 10.2, step 6).
/// </summary>
public sealed record SqlEvidence
{
    public required string QueryId { get; init; }

    public required double ElapsedMs { get; init; }

    public int? RowCount { get; init; }

    public required IReadOnlyList<string> ParameterNames { get; init; }

    public string? Error { get; init; }
}

public static class SqlEvidenceWriter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static async Task WriteAsync(string testDirectory, IReadOnlyList<SqlEvidence> evidence, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testDirectory);
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.Count == 0)
        {
            return;
        }

        Directory.CreateDirectory(testDirectory);
        string path = Path.Combine(testDirectory, ArtifactNames.SqlEvidence);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(evidence, Options), cancellationToken).ConfigureAwait(false);
    }
}
