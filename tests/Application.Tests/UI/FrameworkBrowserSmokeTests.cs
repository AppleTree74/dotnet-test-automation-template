using Allure.NUnit;
using Allure.NUnit.Attributes;
using Application.Tests.Framework;
using Automation.Core.Identity;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Application.Tests.UI;

/// <summary>
/// Framework self-test that exercises the full browser stack — fresh context, page, locators,
/// auto-waiting, evidence pipeline — without any external Test URL. It uses in-memory page content
/// so it runs anywhere and proves the Chromium (and Firefox/WebKit) path end to end. Product UI
/// tests instead navigate to the configured Test URL and remain skipped until one is supplied.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureEpic("Framework")]
[AllureFeature("Browser self-test")]
[TestType(TestType.UI)]
[Suite(Suite.Smoke)]
[Feature("Framework")]
public sealed class FrameworkBrowserSmokeTests : UiTestBase
{
    [Test]
    [AllureStory("A fresh page renders and locators resolve")]
    public async Task Page_RendersContent_AndLocatorsResolve()
    {
        await Page.SetContentAsync(
            """
            <main>
              <h1>Welcome</h1>
              <button type="button">Sign in</button>
            </main>
            """);

        await Expect(Page.GetByRole(AriaRole.Heading)).ToHaveTextAsync("Welcome");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" })).ToBeVisibleAsync();
    }

    private static IPageAssertions Expect(IPage page) => Assertions.Expect(page);

    private static ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);
}
