using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Automation.Core.Identity;

/// <summary>
/// Produces stable, filesystem-safe identifiers from arbitrary text. Used for
/// <c>TestId</c> and any other name that becomes a directory or file segment.
/// </summary>
public static class FileSystemName
{
    private const int MaxSegmentLength = 120;

    /// <summary>
    /// Sanitizes <paramref name="value"/> into a lowercase, filesystem-safe segment.
    /// A short stable hash of the original value is appended so that two inputs which
    /// collapse to the same sanitized text still receive distinct identifiers.
    /// </summary>
    public static string Sanitize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var builder = new StringBuilder(value.Length);
        foreach (char c in value)
        {
            bool safe = char.IsAsciiLetterOrDigit(c);
            if (safe)
            {
                builder.Append(char.ToLowerInvariant(c));
            }
            else if (c is '.' or '-' or '_' or '+' or '(' or ')')
            {
                builder.Append('-');
            }
            else if (char.IsWhiteSpace(c))
            {
                builder.Append('-');
            }

            // All other characters (path separators, wildcards, control chars) are dropped.
        }

        string collapsed = CollapseDashes(builder.ToString()).Trim('-');
        if (collapsed.Length == 0)
        {
            collapsed = "item";
        }

        string hash = ShortHash(value);
        int budget = MaxSegmentLength - hash.Length - 1;
        if (collapsed.Length > budget)
        {
            collapsed = collapsed[..budget].Trim('-');
        }

        return $"{collapsed}-{hash}";
    }

    /// <summary>Returns an 8-character lowercase hex hash of <paramref name="value"/>.</summary>
    public static string ShortHash(string value)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        var sb = new StringBuilder(8);
        for (int i = 0; i < 4; i++)
        {
            sb.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    private static string CollapseDashes(string value)
    {
        var sb = new StringBuilder(value.Length);
        bool lastDash = false;
        foreach (char c in value)
        {
            if (c == '-')
            {
                if (!lastDash)
                {
                    sb.Append(c);
                }

                lastDash = true;
            }
            else
            {
                sb.Append(c);
                lastDash = false;
            }
        }

        return sb.ToString();
    }
}
