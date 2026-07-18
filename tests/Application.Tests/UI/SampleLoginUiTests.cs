using Allure.NUnit;
using Allure.NUnit.Attributes;
using Application.Automation.Pages;
using Application.Tests.Framework;
using Automation.Core.Identity;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Application.Tests.UI;

/// <summary>
/// Sample product UI test. Skipped until a real Test URL and the expected sign-in outcome are
/// supplied (guide section 2). Remove the <see cref="IgnoreAttribute"/> once configured.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureEpic("Customer")]
[AllureFeature("Authentication")]
[TestType(TestType.UI)]
[Suite(Suite.Smoke)]
[Feature("Login")]
[Ignore("Sample: configure Browser:BaseUrl and the expected login outcome, then enable.")]
public sealed class SampleLoginUiTests : UiTestBase
{
    [Test]
    [AllureStory("A registered customer can sign in")]
    public async Task RegisteredCustomer_CanSignIn()
    {
        var login = new LoginPage(Page);
        await login.GotoAsync();
        await login.SignInAsync("REPLACE_WITH_TEST_USER", "REPLACE_WITH_TEST_SECRET");

        await Assertions.Expect(login.WelcomeHeading).ToBeVisibleAsync();
    }
}
