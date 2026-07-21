using System.Text.Json;
using System.Text.Json.Nodes;
using Automation.Core.Redaction;

namespace Automation.Core.Reporting;

/// <summary>
/// Produces a sanitized copy of an Allure results directory for publication (P1-3). The raw Allure
/// result JSON carries free-text the attachment policy never sees — <c>statusDetails.message</c> and
/// <c>statusDetails.trace</c> (Playwright/NUnit failure text, which can quote DOM, ARIA snapshots,
/// locators, and on-screen values), plus parameters, labels, and step names. This walks every JSON
/// value through the shared <see cref="IRedactor"/> so the report and its history are generated from
/// sanitized input while the untouched raw results remain in the restricted workflow artifacts.
/// The transform preserves JSON structure and only ever replaces string contents; it fails closed
/// (throws) rather than emitting a partially transformed file.
/// </summary>
public static class AllureResultSanitizer
{
    // Text evidence that is redacted line-for-line; anything else is copied verbatim (binary
    // artifacts are gated separately by the attachment policy).
    private static readonly HashSet<string> TextAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".jsonl", ".html", ".htm", ".csv", ".log", ".xml",
    };

    /// <summary>
    /// Sanitizes every file in <paramref name="inputDirectory"/> into <paramref name="outputDirectory"/>:
    /// <c>.json</c> results are structurally redacted, other text evidence is redacted as free text,
    /// and binary files are copied unchanged. Throws on any read/parse/transform error so a broken
    /// file aborts publication instead of leaking.
    /// </summary>
    public static void SanitizeDirectory(string inputDirectory, string outputDirectory, IRedactor redactor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentNullException.ThrowIfNull(redactor);

        if (!Directory.Exists(inputDirectory))
        {
            throw new DirectoryNotFoundException($"Allure results directory not found: '{inputDirectory}'.");
        }

        Directory.CreateDirectory(outputDirectory);

        foreach (string sourcePath in Directory.EnumerateFiles(inputDirectory))
        {
            string fileName = Path.GetFileName(sourcePath);
            string destinationPath = Path.Combine(outputDirectory, fileName);
            string extension = Path.GetExtension(fileName);

            if (string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
            {
                string json = File.ReadAllText(sourcePath);
                File.WriteAllText(destinationPath, SanitizeJson(json, redactor));
            }
            else if (TextAttachmentExtensions.Contains(extension))
            {
                File.WriteAllText(destinationPath, redactor.RedactText(File.ReadAllText(sourcePath)));
            }
            else
            {
                File.Copy(sourcePath, destinationPath, overwrite: true);
            }
        }
    }

    /// <summary>
    /// Returns <paramref name="json"/> with every string value redacted, preserving structure.
    /// Throws <see cref="JsonException"/> on invalid JSON (fail closed).
    /// </summary>
    public static string SanitizeJson(string json, IRedactor redactor)
    {
        ArgumentNullException.ThrowIfNull(redactor);

        JsonNode? root = JsonNode.Parse(json)
            ?? throw new JsonException("Allure result JSON parsed to null.");

        JsonNode? sanitized = RedactNode(root, redactor);
        return sanitized?.ToJsonString() ?? "null";
    }

    private static JsonNode? RedactNode(JsonNode? node, IRedactor redactor)
    {
        switch (node)
        {
            case JsonObject obj:
                var redactedObject = new JsonObject();
                foreach (KeyValuePair<string, JsonNode?> property in obj)
                {
                    redactedObject[property.Key] = RedactNode(property.Value, redactor);
                }

                return redactedObject;

            case JsonArray array:
                var redactedArray = new JsonArray();
                foreach (JsonNode? item in array)
                {
                    redactedArray.Add(RedactNode(item, redactor));
                }

                return redactedArray;

            case JsonValue value:
                return value.TryGetValue(out string? text)
                    ? JsonValue.Create(redactor.RedactText(text))
                    : value.DeepClone();

            default:
                return node?.DeepClone();
        }
    }
}
