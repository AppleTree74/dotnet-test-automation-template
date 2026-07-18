using Automation.Browser;
using Microsoft.Playwright;

namespace Application.Automation.Components;

/// <summary>
/// Sample Component Object for a reusable navigation region. Components model reusable areas
/// scoped to a root locator and, like Page Objects, contain no assertions.
/// </summary>
public sealed class PrimaryNavigation : ComponentObject
{
    public PrimaryNavigation(IPage page)
        : base(page.GetByRole(AriaRole.Navigation))
    {
    }

    public Task OpenAsync(string linkName) => ByRole(AriaRole.Link, linkName).ClickAsync();

    public ILocator SignOutButton => ByRole(AriaRole.Button, "Sign out");
}
