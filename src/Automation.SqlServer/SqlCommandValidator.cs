using System.Text;
using System.Text.RegularExpressions;
using Automation.Core.Exceptions;

namespace Automation.SqlServer;

/// <summary>
/// Defense-in-depth check that a command is a single read-only query before it reaches the
/// database (guide section 10.2, step 5). This is one layer only; the authoritative control is a
/// database identity granted just SELECT. The validator rejects multiple batches, DML/DDL, stored
/// procedure execution, and other unsafe forms.
/// </summary>
public static partial class SqlCommandValidator
{
    private static readonly string[] ForbiddenKeywords =
    [
        "INSERT", "UPDATE", "DELETE", "MERGE", "UPSERT", "DROP", "ALTER", "CREATE", "TRUNCATE",
        "EXEC", "EXECUTE", "GRANT", "REVOKE", "DENY", "BACKUP", "RESTORE", "SHUTDOWN",
        "RECONFIGURE", "DBCC", "KILL", "WAITFOR", "OPENROWSET", "OPENQUERY", "OPENDATASOURCE",
        "BULK", "INTO", "GO",
    ];

    /// <summary>Validates <paramref name="sql"/> and returns it unchanged, or throws <see cref="UnsafeSqlException"/>.</summary>
    public static string Validate(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        string scrubbed = Scrub(sql).Trim();
        if (scrubbed.Length == 0)
        {
            throw new UnsafeSqlException("Query is empty after removing comments and string literals.");
        }

        // Reject interior statement separators (multiple batches).
        string withoutTrailing = scrubbed.TrimEnd(';', ' ', '\t', '\r', '\n');
        if (withoutTrailing.Contains(';', StringComparison.Ordinal))
        {
            throw new UnsafeSqlException("Multiple SQL statements are not permitted; submit a single read-only query.");
        }

        // Must be a read query.
        if (!StartsWithSelectOrCte(withoutTrailing))
        {
            throw new UnsafeSqlException("Only SELECT (optionally preceded by a WITH CTE) queries are permitted.");
        }

        // Reject any forbidden keyword as a whole word.
        foreach (string keyword in ForbiddenKeywords)
        {
            if (Regex.IsMatch(withoutTrailing, $@"\b{keyword}\b", RegexOptions.IgnoreCase))
            {
                throw new UnsafeSqlException($"The keyword '{keyword}' is not allowed in a read-only query.");
            }
        }

        // Reject stored-procedure name tokens (sp_ / xp_).
        if (ProcTokenRegex().IsMatch(withoutTrailing))
        {
            throw new UnsafeSqlException("Stored-procedure execution is not permitted.");
        }

        // Reject sequence value generation, which advances database state even though it starts
        // with SELECT (e.g. SELECT NEXT VALUE FOR dbo.SomeSequence).
        if (SequenceRegex().IsMatch(withoutTrailing))
        {
            throw new UnsafeSqlException("Sequence value generation (NEXT VALUE FOR) changes state and is not permitted.");
        }

        return sql;
    }

    public static bool IsValid(string sql)
    {
        try
        {
            Validate(sql);
            return true;
        }
        catch (UnsafeSqlException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool StartsWithSelectOrCte(string scrubbed)
    {
        string trimmed = scrubbed.TrimStart('(', ' ', '\t', '\r', '\n');
        return trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Removes line/block comments and neutralizes single-quoted string literals.</summary>
    private static string Scrub(string sql)
    {
        var builder = new StringBuilder(sql.Length);
        for (int i = 0; i < sql.Length; i++)
        {
            char c = sql[i];

            // Line comment.
            if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                while (i < sql.Length && sql[i] != '\n')
                {
                    i++;
                }

                builder.Append(' ');
                continue;
            }

            // Block comment.
            if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < sql.Length && !(sql[i] == '*' && sql[i + 1] == '/'))
                {
                    i++;
                }

                i++; // land on '/'
                builder.Append(' ');
                continue;
            }

            // String literal — replace contents so embedded keywords are ignored.
            if (c == '\'')
            {
                i++;
                while (i < sql.Length)
                {
                    if (sql[i] == '\'' && i + 1 < sql.Length && sql[i + 1] == '\'')
                    {
                        i += 2; // escaped quote
                        continue;
                    }

                    if (sql[i] == '\'')
                    {
                        break;
                    }

                    i++;
                }

                builder.Append("''");
                continue;
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    [GeneratedRegex(@"\b(sp|xp)_\w+", RegexOptions.IgnoreCase)]
    private static partial Regex ProcTokenRegex();

    [GeneratedRegex(@"\bNEXT\s+VALUE\s+FOR\b", RegexOptions.IgnoreCase)]
    private static partial Regex SequenceRegex();
}
