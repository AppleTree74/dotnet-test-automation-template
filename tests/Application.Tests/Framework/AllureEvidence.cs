using Allure.Net.Commons;
using Microsoft.Extensions.Logging;

namespace Application.Tests.Framework;

/// <summary>
/// Attaches raw evidence files to the current Allure test result. Isolated here so the Allure API
/// surface is referenced in exactly one place. Failures never fail a test, but they are logged so
/// missing evidence can be diagnosed.
/// </summary>
public static class AllureEvidence
{
    public static void AttachDirectory(string directory, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        foreach (string path in Directory.EnumerateFiles(directory))
        {
            try
            {
                AllureApi.AddAttachment(path, Path.GetFileName(path));
            }
            catch (Exception ex)
            {
                // Never fail a test over an attachment; the file still exists on disk. Log the
                // exception type only (no message) so nothing sensitive is emitted.
                logger.LogWarning(
                    "Failed to attach evidence file {File} to Allure ({Error}); it remains on disk.",
                    Path.GetFileName(path),
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
            AllureApi.AddTestParameter("browser", browser);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to set the Allure 'browser' parameter ({Error}).", ex.GetType().Name);
        }
    }
}
