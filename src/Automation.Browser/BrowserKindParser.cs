using Automation.Core.Configuration;

namespace Automation.Browser;

/// <summary>Parses allow-listed browser names into <see cref="BrowserKind"/>. Never trusts free text.</summary>
public static class BrowserKindParser
{
    public static bool TryParse(string? value, out BrowserKind kind)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "chromium":
                kind = BrowserKind.Chromium;
                return true;
            case "firefox":
                kind = BrowserKind.Firefox;
                return true;
            case "webkit":
                kind = BrowserKind.Webkit;
                return true;
            default:
                kind = BrowserKind.Chromium;
                return false;
        }
    }

    public static BrowserKind Parse(string? value) =>
        TryParse(value, out BrowserKind kind)
            ? kind
            : throw new ArgumentException($"Unsupported browser '{value}'. Allowed: chromium, firefox, webkit.", nameof(value));
}
