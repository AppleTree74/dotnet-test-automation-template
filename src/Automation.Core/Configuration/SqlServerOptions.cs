using System.ComponentModel.DataAnnotations;

namespace Automation.Core.Configuration;

/// <summary>
/// SQL Server configuration. The connection string is a secret and is used by a read-only
/// identity. The framework exposes parameterized query operations only (guide section 10).
/// </summary>
public sealed class SqlServerOptions
{
    public const string SectionName = "SqlServer";

    /// <summary>
    /// Full connection string. Secret. Supplied via user-secrets or a GitHub Environment
    /// secret; blank in the committed template. The credential MUST have only SELECT rights.
    /// </summary>
    public string? ConnectionString { get; init; }

    [Range(1, 600)]
    public int CommandTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Apply <c>ApplicationIntent=ReadOnly</c> when the string does not already set it.
    /// This is routing intent, not authorization; database permissions are authoritative.
    /// </summary>
    public bool ApplyReadOnlyIntent { get; init; } = true;

    public bool IsPlaceholder() => string.IsNullOrWhiteSpace(ConnectionString);
}
