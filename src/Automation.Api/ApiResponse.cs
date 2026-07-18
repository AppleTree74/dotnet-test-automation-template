using System.Net;

namespace Automation.Api;

/// <summary>
/// Typed result of an API call. Carries the deserialized payload, status, headers, and
/// sanitized diagnostics so a test can assert without the client ever asserting itself.
/// </summary>
public sealed record ApiResponse<T>
{
    public required HttpStatusCode StatusCode { get; init; }

    public bool IsSuccess => (int)StatusCode is >= 200 and < 300;

    public T? Data { get; init; }

    /// <summary>Bounded raw body text, retained for assertions and diagnostics.</summary>
    public string? RawBody { get; init; }

    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public required ApiRequestDiagnostics Diagnostics { get; init; }
}
