using Allure.NUnit;
using Allure.NUnit.Attributes;
using Application.Automation.Api;
using Application.Automation.Api.Dtos;
using Application.Tests.Framework;
using Automation.Api;
using Automation.Core.Identity;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Application.Tests.API;

/// <summary>
/// Sample API test. Runs without a browser. Skipped until the API base URL and bearer token are
/// configured (guide section 2). Remove the <see cref="IgnoreAttribute"/> once configured.
/// </summary>
[TestFixture]
[AllureNUnit]
[AllureEpic("Customer")]
[AllureFeature("Orders API")]
[TestType(TestType.API)]
[Suite(Suite.Regression)]
[Feature("Orders")]
[Ignore("Sample: configure Api:BaseUrl and Api:BearerToken, then enable.")]
public sealed class SampleOrdersApiTests : ApiTestBase
{
    [Test]
    [AllureStory("An existing order can be retrieved")]
    public async Task ExistingOrder_CanBeRetrieved()
    {
        var orders = Services.GetRequiredService<OrdersApiClient>();

        ApiResponse<OrderDto> response = await orders.GetOrderAsync("REPLACE_WITH_ORDER_ID");

        Assert.Multiple(() =>
        {
            Assert.That(response.IsSuccess, Is.True);
            Assert.That(response.Data, Is.Not.Null);
        });
    }
}
