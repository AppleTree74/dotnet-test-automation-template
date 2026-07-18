using Microsoft.Playwright;

namespace Automation.Browser;

/// <summary>
/// Base for a Page Object that models a whole page. Public methods express user behaviour
/// (e.g. <c>SignInAsync</c>), not low-level click sequences. Page Objects MUST NOT make NUnit
/// assertions and MUST NOT introduce fixed sleeps; rely on Playwright auto-waiting
/// (guide section 8.2).
/// </summary>
public abstract class PageObject
{
    protected PageObject(IPage page)
    {
        Page = page ?? throw new ArgumentNullException(nameof(page));
    }

    protected IPage Page { get; }

    /// <summary>Relative path from the configured base URL, e.g. <c>/login</c>. Optional.</summary>
    protected virtual string? Path => null;

    /// <summary>Navigates to <see cref="Path"/> (resolved against the context base URL).</summary>
    public virtual async Task GotoAsync()
    {
        if (Path is null)
        {
            throw new InvalidOperationException($"{GetType().Name} does not define a Path to navigate to.");
        }

        await Page.GotoAsync(Path).ConfigureAwait(false);
    }

    // Locator preference order: role, label, placeholder, text, test id, stable CSS, XPath last.
    protected ILocator ByRole(AriaRole role, string? name = null) =>
        name is null ? Page.GetByRole(role) : Page.GetByRole(role, new PageGetByRoleOptions { Name = name });

    protected ILocator ByLabel(string text) => Page.GetByLabel(text);

    protected ILocator ByPlaceholder(string text) => Page.GetByPlaceholder(text);

    protected ILocator ByText(string text) => Page.GetByText(text);

    protected ILocator ByTestId(string testId) => Page.GetByTestId(testId);
}
