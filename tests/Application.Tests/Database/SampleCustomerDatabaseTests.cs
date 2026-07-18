using Allure.NUnit;
using Allure.NUnit.Attributes;
using Application.Automation.Database;
using Application.Automation.Database.Dtos;
using Application.Tests.Framework;
using Automation.Core.Identity;
using NUnit.Framework;

namespace Application.Tests.Database;

/// <summary>
/// Sample Database test. Runs without a browser and verifies state only (read-only). Skipped until
/// a read-only SQL connection string is configured (guide section 2). Database access verifies
/// state; it never creates, repairs, or removes it.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureEpic("Customer")]
[AllureFeature("Customer data")]
[TestType(TestType.Database)]
[Suite(Suite.Regression)]
[Feature("Customer")]
[Ignore("Sample: configure SqlServer:ConnectionString with a read-only identity, then enable.")]
public sealed class SampleCustomerDatabaseTests : DatabaseTestBase
{
    [Test]
    [AllureStory("A known customer exists and is active")]
    public async Task KnownCustomer_IsActive()
    {
        IReadOnlyList<CustomerRecord> rows = await QueryAsync<CustomerRecord>(
            CustomerQueries.GetById("REPLACE_WITH_CUSTOMER_ID"));

        Assert.That(rows, Is.Not.Empty);
        Assert.That(rows[0].Status, Is.EqualTo("active"));
    }
}
