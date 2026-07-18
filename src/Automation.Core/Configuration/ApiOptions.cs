using System.ComponentModel.DataAnnotations;

namespace Automation.Core.Configuration;

/// <summary>
/// REST API configuration. Bearer authentication is the initial strategy and is applied
/// through a delegating handler, never through mutable shared default headers.
/// </summary>
public sealed class ApiOptions
{
    public const string SectionName = "Api";

    [Required]
    [Url]
    public string BaseUrl { get; init; } = "https://api.test.example.invalid";

    [Range(1_000, 300_000)]
    public int TimeoutMs { get; init; } = 30_000;

    /// <summary>
    /// Bearer token. Secret. Supplied via user-secrets locally or a GitHub Environment
    /// secret in CI; blank in the committed template. Never commit a real value.
    /// </summary>
    public string? BearerToken { get; init; }

    public bool IsPlaceholder() =>
        BaseUrl.Contains(".invalid", StringComparison.OrdinalIgnoreCase)
        || string.IsNullOrWhiteSpace(BearerToken);
}
