using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Automation.Core.Configuration;
using Automation.Core.Redaction;
using Microsoft.Extensions.Logging;

namespace Automation.Api;

/// <summary>
/// Default <see cref="IApiClient"/>. Uses an <c>IHttpClientFactory</c>-provided
/// <see cref="HttpClient"/>, System.Text.Json, explicit timeouts and cancellation, and produces
/// sanitized diagnostics. It never asserts and never auto-retries.
/// </summary>
public sealed class ApiClient : IApiClient
{
    public const string CorrelationIdHeader = "X-Correlation-ID";
    private const int MaxEvidenceBodyLength = 8 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IRedactor _redactor;
    private readonly ApiOptions _options;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient httpClient, IRedactor redactor, ApiOptions options, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiResponse<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string correlationId = EnsureCorrelationId(request);
        string sanitizedUrl = _redactor.RedactUrl(request.RequestUri?.ToString());
        var stopwatch = Stopwatch.StartNew();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_options.TimeoutMs));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            using HttpResponseMessage response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseContentRead, linkedCts.Token)
                .ConfigureAwait(false);

            stopwatch.Stop();
            string body = await response.Content.ReadAsStringAsync(linkedCts.Token).ConfigureAwait(false);
            bool success = (int)response.StatusCode is >= 200 and < 300;

            var diagnostics = new ApiRequestDiagnostics
            {
                Method = request.Method.Method,
                SanitizedUrl = sanitizedUrl,
                StatusCode = (int)response.StatusCode,
                ElapsedMs = stopwatch.Elapsed.TotalMilliseconds,
                CorrelationId = correlationId,
                SanitizedResponseBody = success ? null : BoundedSanitizedBody(body),
            };

            _logger.LogInformation(
                "{Method} {Url} -> {Status} in {Elapsed:F0} ms (correlation {CorrelationId}).",
                diagnostics.Method,
                sanitizedUrl,
                (int)response.StatusCode,
                diagnostics.ElapsedMs,
                correlationId);

            return new ApiResponse<T>
            {
                StatusCode = response.StatusCode,
                Data = Deserialize<T>(body, success),
                RawBody = BoundedSanitizedBody(body),
                Headers = CollectHeaders(response),
                Diagnostics = diagnostics,
            };
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return TimeoutResponse<T>(request, sanitizedUrl, correlationId, stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            // Transport failures (DNS, connection refused, TLS) never reach an HTTP status. Surface
            // a sanitized, bounded diagnostic so api-evidence.json is produced instead of an
            // unhandled exception escaping before any diagnostic is recorded.
            stopwatch.Stop();
            return TransportFailureResponse<T>(request, sanitizedUrl, correlationId, stopwatch.Elapsed.TotalMilliseconds, ex);
        }
    }

    private static string EnsureCorrelationId(HttpRequestMessage request)
    {
        if (request.Headers.TryGetValues(CorrelationIdHeader, out IEnumerable<string>? values))
        {
            string? existing = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }
        }

        string id = Guid.NewGuid().ToString("N");
        request.Headers.Remove(CorrelationIdHeader);
        request.Headers.Add(CorrelationIdHeader, id);
        return id;
    }

    private static T? Deserialize<T>(string body, bool success)
    {
        if (!success || string.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        if (typeof(T) == typeof(string))
        {
            return (T)(object)body;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private Dictionary<string, string> CollectHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers)
        {
            headers[header.Key] = _redactor.RedactHeaderValue(header.Key, string.Join(", ", header.Value));
        }

        foreach (var header in response.Content.Headers)
        {
            headers[header.Key] = _redactor.RedactHeaderValue(header.Key, string.Join(", ", header.Value));
        }

        return headers;
    }

    private string BoundedSanitizedBody(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return string.Empty;
        }

        string sanitized = _redactor.RedactJson(body);
        return sanitized.Length <= MaxEvidenceBodyLength
            ? sanitized
            : sanitized[..MaxEvidenceBodyLength] + "…[truncated]";
    }

    private ApiResponse<T> TimeoutResponse<T>(HttpRequestMessage request, string sanitizedUrl, string correlationId, double elapsedMs)
    {
        _logger.LogWarning("{Method} {Url} timed out after {Elapsed:F0} ms.", request.Method.Method, sanitizedUrl, elapsedMs);
        return new ApiResponse<T>
        {
            StatusCode = HttpStatusCode.RequestTimeout,
            Diagnostics = new ApiRequestDiagnostics
            {
                Method = request.Method.Method,
                SanitizedUrl = sanitizedUrl,
                StatusCode = null,
                ElapsedMs = elapsedMs,
                CorrelationId = correlationId,
                Error = "Request exceeded the configured timeout.",
            },
        };
    }

    private ApiResponse<T> TransportFailureResponse<T>(
        HttpRequestMessage request,
        string sanitizedUrl,
        string correlationId,
        double elapsedMs,
        HttpRequestException exception)
    {
        string category = exception.HttpRequestError.ToString();
        string message = _redactor.RedactText(exception.Message);
        if (message.Length > 512)
        {
            message = message[..512] + "…";
        }

        // Log the category and sanitized message only — never the raw exception object.
        _logger.LogWarning(
            "{Method} {Url} failed at transport ({Category}) after {Elapsed:F0} ms.",
            request.Method.Method,
            sanitizedUrl,
            category,
            elapsedMs);

        return new ApiResponse<T>
        {
            StatusCode = (HttpStatusCode)0,
            Diagnostics = new ApiRequestDiagnostics
            {
                Method = request.Method.Method,
                SanitizedUrl = sanitizedUrl,
                StatusCode = null,
                ElapsedMs = elapsedMs,
                CorrelationId = correlationId,
                Error = $"Transport failure ({category}): {message}",
            },
        };
    }
}
