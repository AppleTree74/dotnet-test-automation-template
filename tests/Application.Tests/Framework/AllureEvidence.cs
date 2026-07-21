using Allure.Net.Commons;
using Automation.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Tests.Framework;

/// <summary>
/// Attaches raw evidence files to the current Allure test result. Isolated here so the Allure API
/// surface is referenced in exactly one place. Failures never fail a test, but they are logged so
/// missing evidence can be diagnosed.
/// </summary>
public static class AllureEvidence
{
    /// <summary>
    /// Attaches evidence files from a test directory to the current Allure result, consulting
    /// <paramref name="options"/> for each file. Raw binary evidence (screenshot, trace, HAR, video)
    /// cannot be centrally redacted, so publishing it to the report/Pages is policy-gated by artifact
    /// type via <see cref="ArtifactOptions.ShouldAttachToReport"/> (P1-01/P1-2). Withheld files remain
    /// on disk and in the restricted CI workflow artifacts. Only files directly inside
    /// <paramref name="directory"/> are considered.
    /// </summary>
    public static void AttachDirectory(string directory, ArtifactOptions options, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        foreach (string path in Directory.EnumerateFiles(directory))
        {
            string fileName = Path.GetFileName(path);
            if (!options.ShouldAttachToReport(fileName))
            {
                logger.LogInformation(
                    "Evidence file {File} is retained on disk but not attached to the report by policy.",
                    fileName);
                continue;
            }

            try
            {
                AllureApi.AddAttachment(path, fileName);
            }
            catch (Exception ex)
            {
                // Never fail a test over an attachment; the file still exists on disk. Log the
                // exception type only (no message) so nothing sensitive is emitted.
                logger.LogWarning(
                    "Failed to attach evidence file {File} to Allure ({Error}); it remains on disk.",
                    fileName,
                    ex.GetType().Name);
            }
        }
    }

    /// <summary>
    /// Records the browser as a test parameter so that a multi-browser run (`-Browser all`) yields
    /// distinct, comparable results per browser in the one aggregated report.
    /// </summary>
    public static void SetBrowserParameter(string browser, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        if (string.IsNullOrWhiteSpace(browser))
        {
            return;
        }

        try
        {
            // Use the Parameter overload with an explicit string value. The object overload
            // (AddTestParameter(string, object)) serializes the value through the JSON formatter,
            // which renders a plain string as a quoted "chromium"; setting Parameter.value directly
            // stores the bare token while still producing distinct per-browser history IDs (P3-01).
            AllureApi.AddTestParameter(new Parameter { name = "browser", value = browser });
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to set the Allure 'browser' parameter ({Error}).", ex.GetType().Name);
        }
    }
}
