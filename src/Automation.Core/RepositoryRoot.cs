namespace Automation.Core;

/// <summary>
/// Locates the repository root so artifacts and Allure results land in a stable location
/// regardless of the test host's default working directory.
/// </summary>
public static class RepositoryRoot
{
    /// <summary>
    /// Walks up from <paramref name="startDirectory"/> (or the app base directory) looking for a
    /// repository marker: a <c>.slnx</c>/<c>.sln</c> file, or a <c>.git</c> directory. Falls back
    /// to the current directory when no marker is found.
    /// </summary>
    public static string Find(string? startDirectory = null)
    {
        var directory = new DirectoryInfo(startDirectory ?? AppContext.BaseDirectory);

        while (directory is not null)
        {
            bool hasSolution = directory.EnumerateFiles("*.slnx").Any()
                || directory.EnumerateFiles("*.sln").Any();
            bool hasGit = Directory.Exists(System.IO.Path.Combine(directory.FullName, ".git"));

            if (hasSolution || hasGit)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
