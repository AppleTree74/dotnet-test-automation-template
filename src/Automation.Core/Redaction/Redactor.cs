using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Automation.Core.Configuration;

namespace Automation.Core.Redaction;

/// <summary>
/// Default <see cref="IRedactor"/>. Combines always-on rules (bearer tokens, connection-string
/// credentials, sensitive headers) with the configured secret field names.
/// </summary>
public sealed partial class Redactor : IRedactor
{
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization",
        "proxy-authorization",
        "cookie",
        "set-cookie",
        "www-authenticate",
        "x-api-key",
    };

    private readonly HashSet<string> _secretKeys;
    private readonly string _mask;

    public Redactor(RedactionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _mask = options.Mask;
        _secretKeys = new HashSet<string>(options.SecretFieldNames, StringComparer.OrdinalIgnoreCase);
    }

    public string RedactText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        string result = BearerRegex().Replace(text, $"Bearer {_mask}");
        result = ConnectionSecretRegex().Replace(result, m => $"{m.Groups[1].Value}={_mask}");
        result = KeyValueSecretRegex().Replace(result, m => MaskKeyValue(m));
        return result;
    }

    public string RedactHeaderValue(string headerName, string? headerValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headerName);
        if (SensitiveHeaders.Contains(headerName) || _secretKeys.Contains(headerName))
        {
            return _mask;
        }

        return RedactText(headerValue);
    }

    public string RedactJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json ?? string.Empty;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                WriteRedacted(document.RootElement, writer, propertyName: null);
            }

            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }
        catch (JsonException)
        {
            // Not valid JSON — fall back to text redaction so nothing leaks.
            return RedactText(json);
        }
    }

    public string RedactUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url ?? string.Empty;
        }

        int queryIndex = url.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex < 0)
        {
            return url;
        }

        string basePart = url[..queryIndex];
        string query = url[(queryIndex + 1)..];
        var rebuilt = new StringBuilder(basePart);
        rebuilt.Append('?');

        string[] pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < pairs.Length; i++)
        {
            string pair = pairs[i];
            int eq = pair.IndexOf('=', StringComparison.Ordinal);
            if (eq > 0)
            {
                string key = pair[..eq];
                string value = _secretKeys.Contains(Uri.UnescapeDataString(key)) ? _mask : pair[(eq + 1)..];
                rebuilt.Append(key).Append('=').Append(value);
            }
            else
            {
                rebuilt.Append(pair);
            }

            if (i < pairs.Length - 1)
            {
                rebuilt.Append('&');
            }
        }

        return rebuilt.ToString();
    }

    public string RedactConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString ?? string.Empty;
        }

        return ConnectionSecretRegex().Replace(connectionString, m => $"{m.Groups[1].Value}={_mask}");
    }

    private void WriteRedacted(JsonElement element, Utf8JsonWriter writer, string? propertyName)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    if (_secretKeys.Contains(property.Name)
                        && property.Value.ValueKind is not JsonValueKind.Object and not JsonValueKind.Array)
                    {
                        writer.WriteStringValue(_mask);
                    }
                    else
                    {
                        WriteRedacted(property.Value, writer, property.Name);
                    }
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (JsonElement item in element.EnumerateArray())
                {
                    WriteRedacted(item, writer, propertyName);
                }

                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(RedactText(element.GetString()));
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    private string MaskKeyValue(Match match)
    {
        string key = match.Groups["key"].Value;
        return _secretKeys.Contains(key)
            ? $"{key}{match.Groups["sep"].Value}{_mask}"
            : match.Value;
    }

    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.IgnoreCase)]
    private static partial Regex BearerRegex();

    [GeneratedRegex(@"(?i)\b(Password|Pwd|User\s*ID|UID)\s*=\s*[^;]+")]
    private static partial Regex ConnectionSecretRegex();

    [GeneratedRegex(@"(?<key>password|token|access_token|refresh_token|apikey|api_key|client_secret)(?<sep>\s*[:=]\s*)(?:""[^""]*""|'[^']*'|[^\s,;&}]+)", RegexOptions.IgnoreCase)]
    private static partial Regex KeyValueSecretRegex();
}
