using Allure.NUnit;
using Allure.NUnit.Attributes;
using Application.Tests.Framework;
using Automation.Core.Identity;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Application.Tests.UI;

/// <summary>
/// Deliberately failing UI test used to demonstrate the failure-evidence pipeline (screenshot,
/// trace, current URL, browser console, page HTML). Marked <see cref="ExplicitAttribute"/> so it
/// never runs in normal suites; invoke it directly to inspect the evidence it produces:
/// <code>pwsh ./scripts/Invoke-Tests.ps1 -TestName Evidence_OnFailure_IsCaptured</code>
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureEpic("Framework")]
[AllureFeature("Failure evidence")]
[TestType(TestType.UI)]
[Suite(Suite.Regression)]
[Feature("Framework")]
[Explicit("Deliberate failure; run manually to demonstrate evidence capture.")]
public sealed class EvidenceDemoTests : UiTestBase
{
    [Test]
    public async Task Evidence_OnFailure_IsCaptured()
    {
        await Page.SetContentAsync("<main><h1>Actual heading</h1></main>");

        // This assertion fails on purpose to trigger full evidence capture in teardown.
        await Assertions.Expect(Page.GetByRole(AriaRole.Heading))
            .ToHaveTextAsync("Expected heading that is not present");
    }
}
