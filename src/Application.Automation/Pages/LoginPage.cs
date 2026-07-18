using Automation.Browser;
using Microsoft.Playwright;

namespace Application.Automation.Pages;

/// <summary>
/// Sample Page Object for a product login page. This is illustrative scaffolding: replace the
/// path and locators with the generated application's real page and remove the skip from the
/// sample test once a Test URL and expected behaviour are supplied.
/// </summary>
public sealed class LoginPage : PageObject
{
    public LoginPage(IPage page)
        : base(page)
    {
    }

    protected override string Path => "/login";

    // Public methods express user behaviour, not low-level clicks.
    public async Task SignInAsync(string username, string password)
    {
        await ByLabel("Username").FillAsync(username);
        await ByLabel("Password").FillAsync(password);
        await ByRole(AriaRole.Button, "Sign in").ClickAsync();
    }

    public ILocator ErrorBanner => ByRole(AriaRole.Alert);

    public ILocator WelcomeHeading => ByRole(AriaRole.Heading, "Welcome");
}
