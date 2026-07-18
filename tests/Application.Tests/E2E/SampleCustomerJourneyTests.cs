using Allure.NUnit;
using Allure.NUnit.Attributes;
using Application.Automation.Workflows;
using Application.Tests.Framework;
using Automation.Core.Identity;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Application.Tests.E2E;

/// <summary>
/// Sample end-to-end test combining UI, API, and read-only SQL through a workflow. Skipped until
/// the Test URL, API, and SQL are configured (guide section 2). E2E tests still own their unique
/// data and artifact paths and must run in parallel with everything else.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureEpic("Customer")]
[AllureFeature("Customer journey")]
[TestType(TestType.E2E)]
[Suite(Suite.Smoke)]
[Feature("Customer")]
[Ignore("Sample: configure Browser, Api, and SqlServer, then enable.")]
public sealed class SampleCustomerJourneyTests : UiTestBase
{
    [Test]
    [AllureStory("A customer's data and orders reconcile across channels")]
    public async Task CustomerData_ReconcilesAcrossChannels()
    {
        var journey = Services.GetRequiredService<CustomerJourney>();

        CustomerJourneyResult result = await journey.LoadCustomerWithOrdersAsync("REPLACE_WITH_CUSTOMER_ID");

        Assert.That(result.Customer, Is.Not.Null);
        Assert.That(result.Orders, Is.Not.Null);
    }
}
