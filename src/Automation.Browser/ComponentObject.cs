using Microsoft.Playwright;

namespace Automation.Browser;

/// <summary>
/// Base for a Component Object that models a reusable region (navigation bar, data table,
/// dialog) scoped to a root <see cref="ILocator"/>. Like Page Objects, components MUST NOT make
/// NUnit assertions.
/// </summary>
public abstract class ComponentObject
{
    protected ComponentObject(ILocator root)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
    }

    /// <summary>The region root. All component locators are resolved beneath it.</summary>
    protected ILocator Root { get; }

    protected ILocator ByRole(AriaRole role, string? name = null) =>
        name is null ? Root.GetByRole(role) : Root.GetByRole(role, new LocatorGetByRoleOptions { Name = name });

    protected ILocator ByLabel(string text) => Root.GetByLabel(text);

    protected ILocator ByText(string text) => Root.GetByText(text);

    protected ILocator ByTestId(string testId) => Root.GetByTestId(testId);
}
