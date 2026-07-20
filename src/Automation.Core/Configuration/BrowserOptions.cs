using System.ComponentModel.DataAnnotations;

namespace Automation.Core.Configuration;

public enum BrowserKind
{
    Chromium,
    Firefox,
    Webkit,
}

/// <summary>
/// Browser configuration. The template ships with a placeholder <see cref="BaseUrl"/>;
/// sample UI tests skip with an explicit reason until a real Test URL is supplied.
/// </summary>
public sealed class BrowserOptions
{
    public const string SectionName = "Browser";

    /// <summary>Web application Test URL. Placeholder <c>*.invalid</c> hosts never resolve.</summary>
    [Required]
    [Url]
    public string BaseUrl { get; init; } = "https://test.example.invalid";

    public BrowserKind DefaultBrowser { get; init; } = BrowserKind.Chromium;

    public bool Headless { get; init; } = true;

    [Range(1_000, 300_000)]
    public int NavigationTimeoutMs { get; init; } = 30_000;

    [Range(1_000, 300_000)]
    public int ActionTimeoutMs { get; init; } = 15_000;

    /// <summary>Slow-motion delay in ms for local debugging only. Keep 0 in CI.</summary>
    [Range(0, 5_000)]
    public int SlowMoMs { get; init; }

    /// <summary>Opt-in heavy capture. Off by default (guide section 8.3).</summary>
    public bool CaptureVideo { get; init; }

    public bool CaptureHar { get; init; }

    /// <summary>
    /// Capture page HTML on failure (redacted and size-bounded). On by default; set false for
    /// applications whose DOM may hold sensitive data that pattern-based redaction cannot remove.
    /// </summary>
    public bool CapturePageHtml { get; init; } = true;

    /// <summary>Returns true when the configured base URL is still a template placeholder.</summary>
    public bool IsPlaceholder() =>
        BaseUrl.Contains(".invalid", StringComparison.OrdinalIgnoreCase);
}
