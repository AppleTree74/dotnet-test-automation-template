namespace Automation.Core.Artifacts;

/// <summary>Stable, well-known evidence file names within a test directory (guide section 7.2).</summary>
public static class ArtifactNames
{
    public const string TestLog = "test-log.jsonl";
    public const string Screenshot = "screenshot.png";
    public const string Trace = "trace.zip";
    public const string PageHtml = "page.html";
    public const string BrowserConsole = "browser-console.jsonl";
    public const string ApiEvidence = "api-evidence.json";
    public const string SqlEvidence = "sql-evidence.json";
    public const string Video = "video.webm";
    public const string Har = "network.har";
}
