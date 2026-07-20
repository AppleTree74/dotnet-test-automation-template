using System.Text;
using System.Text.Json;
using Automation.Core.Redaction;

namespace Automation.Browser.Evidence;

/// <summary>
/// Redacts browser evidence before it is written to disk and attached to reports (guide section 7.3:
/// redaction happens before logs or attachments). Console text and locations pass through the
/// central redactor; page HTML is best-effort pattern masking (bearer tokens, connection strings,
/// known secret <c>key=value</c>) bounded in size. HTML masking cannot guarantee removal of
/// arbitrary sensitive DOM content, so page-HTML capture is optional (see <c>BrowserOptions</c>),
/// and screenshots cannot be redacted automatically at all.
/// </summary>
public static class BrowserEvidence
{
    /// <summary>Upper bound on captured page HTML.</summary>
    public const int MaxHtmlLength = 512 * 1024;

    /// <summary>Serializes console records to JSONL with text and location redacted.</summary>
    public static string SerializeConsole(IRedactor redactor, IEnumerable<ConsoleMessageRecord> records)
    {
        ArgumentNullException.ThrowIfNull(redactor);
        ArgumentNullException.ThrowIfNull(records);

        var builder = new StringBuilder();
        foreach (ConsoleMessageRecord record in records)
        {
            ConsoleMessageRecord redacted = record with
            {
                Text = redactor.RedactText(record.Text),
                Location = record.Location is null ? null : redactor.RedactUrl(record.Location),
            };
            builder.AppendLine(JsonSerializer.Serialize(redacted));
        }

        return builder.ToString();
    }

    /// <summary>Applies pattern-based secret redaction to page HTML and bounds its length.</summary>
    public static string RedactHtml(IRedactor redactor, string? html)
    {
        ArgumentNullException.ThrowIfNull(redactor);
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        string sanitized = redactor.RedactText(html);
        return sanitized.Length <= MaxHtmlLength
            ? sanitized
            : sanitized[..MaxHtmlLength] + "\n<!-- [truncated] -->";
    }
}
