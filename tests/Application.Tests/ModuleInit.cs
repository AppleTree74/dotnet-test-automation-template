using System.Runtime.CompilerServices;
using Automation.Core;

namespace Application.Tests;

/// <summary>
/// Pins the process working directory to the repository root as early as possible — before NUnit,
/// the Allure adapter, or any other component resolves a relative path. The Allure results
/// directory (<c>allure-results</c>) and the artifacts tree both resolve against the working
/// directory, and a module initializer runs on assembly load, well before test hosts on CI would
/// otherwise leave the working directory pointing at the test binaries.
/// </summary>
internal static class ModuleInit
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        try
        {
            Directory.SetCurrentDirectory(RepositoryRoot.Find());
        }
        catch (Exception)
        {
            // If the repo root cannot be resolved, leave the working directory unchanged; the
            // run still functions, and artifacts fall back to the current directory.
        }
    }
}
