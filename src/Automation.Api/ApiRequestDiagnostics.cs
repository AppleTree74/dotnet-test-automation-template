namespace Automation.Api;

/// <summary>
/// Sanitized record of one API exchange (guide section 9.2). Contains method, sanitized URL,
/// status, elapsed time, correlation id, and bounded sanitized bodies. Never contains raw
/// secrets; the URL and bodies pass through the redactor before construction.
/// </summary>
public sealed record ApiRequestDiagnostics
{
    public required string Method { get; init; }

    public required string SanitizedUrl { get; init; }

    public int? StatusCode { get; init; }

    public required double ElapsedMs { get; init; }

    public required string CorrelationId { get; init; }

    /// <summary>Populated with a bounded, sanitized body only on failure.</summary>
    public string? SanitizedResponseBody { get; init; }

    public string? Error { get; init; }
}
