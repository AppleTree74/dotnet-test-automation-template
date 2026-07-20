using Allure.Net.Commons;

namespace Application.Tests.Framework;

/// <summary>
/// Attaches raw evidence files to the current Allure test result. Isolated here so the Allure API
/// surface is referenced in exactly one place.
/// </summary>
public static class AllureEvidence
{
    public static void AttachDirectory(string directory)
    {
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
            catch (Exception)
            {
                // Allure attachment failures must never fail a test; evidence still exists on disk.
            }
        }
    }

    /// <summary>
    /// Records the browser as a test parameter so that a multi-browser run (`-Browser all`) yields
    /// distinct, comparable results per browser in the one aggregated report.
    /// </summary>
    public static void SetBrowserParameter(string browser)
    {
        if (string.IsNullOrWhiteSpace(browser))
        {
            return;
        }

        try
        {
            AllureApi.AddTestParameter("browser", browser);
        }
        catch (Exception)
        {
            // Parameter tagging is best-effort; never fail a test because of Allure metadata.
        }
    }
}
