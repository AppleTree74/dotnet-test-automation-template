using System.Text.Json;
using Automation.Core.Configuration;
using Automation.Core.Redaction;
using Automation.Core.Reporting;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: AllureResultsSanitizer <input-results-dir> <output-results-dir>");
    return 2;
}

string inputDirectory = args[0];
string outputDirectory = args[1];

try
{
    // The built-in redaction patterns (bearer tokens, connection secrets, secret key=value) apply
    // regardless of configured field names, so default options are sufficient for the report copy.
    var redactor = new Redactor(new RedactionOptions());
    AllureResultSanitizer.SanitizeDirectory(inputDirectory, outputDirectory, redactor);
    Console.WriteLine($"Sanitized Allure results '{inputDirectory}' -> '{outputDirectory}'.");
    return 0;
}
catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException or ArgumentException)
{
    // Fail closed: never publish from partially transformed input.
    Console.Error.WriteLine($"Allure results sanitization failed ({ex.GetType().Name}): {ex.Message}");
    return 1;
}
