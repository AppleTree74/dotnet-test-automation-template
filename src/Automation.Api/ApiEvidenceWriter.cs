using System.Text.Json;
using Automation.Core.Artifacts;

namespace Automation.Api;

/// <summary>Writes sanitized API diagnostics to <c>api-evidence.json</c> in a test directory.</summary>
public static class ApiEvidenceWriter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static async Task WriteAsync(string testDirectory, IReadOnlyList<ApiRequestDiagnostics> diagnostics, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testDirectory);
        ArgumentNullException.ThrowIfNull(diagnostics);
        if (diagnostics.Count == 0)
        {
            return;
        }

        Directory.CreateDirectory(testDirectory);
        string path = Path.Combine(testDirectory, ArtifactNames.ApiEvidence);
        string json = JsonSerializer.Serialize(diagnostics, Options);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }

    public static Task WriteAsync(string testDirectory, ApiRequestDiagnostics diagnostics, CancellationToken cancellationToken = default) =>
        WriteAsync(testDirectory, [diagnostics], cancellationToken);
}
