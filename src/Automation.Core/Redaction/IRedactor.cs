namespace Automation.Core.Redaction;

/// <summary>
/// Removes secrets before any text reaches a log, artifact, or report. Redaction is a
/// primary control; GitHub log masking is not relied upon (guide section 7.3).
/// </summary>
public interface IRedactor
{
    /// <summary>Masks bearer tokens, connection-string credentials, and known secret keys in free text.</summary>
    string RedactText(string? text);

    /// <summary>Masks a header value when the header name is sensitive; otherwise returns it unchanged.</summary>
    string RedactHeaderValue(string headerName, string? headerValue);

    /// <summary>Masks values of secret-named properties within a JSON document.</summary>
    string RedactJson(string? json);

    /// <summary>Masks secret query-string parameters and never leaks the raw query.</summary>
    string RedactUrl(string? url);

    /// <summary>Masks credential tokens (Password/User ID/…) inside a SQL connection string.</summary>
    string RedactConnectionString(string? connectionString);
}
