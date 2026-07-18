namespace Automation.Core.Configuration;

/// <summary>
/// Configures the redactor. The always-on rules (Authorization, cookies, bearer tokens,
/// connection strings) are enforced in code; this adds product-specific secret field names.
/// </summary>
public sealed class RedactionOptions
{
    public const string SectionName = "Redaction";

    /// <summary>
    /// JSON property names and query/form keys whose values must be masked, in addition to
    /// the built-in defaults. Matching is case-insensitive.
    /// </summary>
    public IReadOnlyList<string> SecretFieldNames { get; init; } =
    [
        "password",
        "token",
        "access_token",
        "refresh_token",
        "authorization",
        "apikey",
        "api_key",
        "client_secret",
        "connectionstring",
        "connection_string",
    ];

    /// <summary>Replacement text written in place of a secret value.</summary>
    public string Mask { get; init; } = "***REDACTED***";
}
