using Automation.Core.Configuration;
using Automation.Core.Exceptions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Automation.UnitTests.Core;

[TestFixture]
public sealed class OptionsValidationTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Test]
    public void BindAndValidate_AcceptsPlaceholderUrls()
    {
        IConfiguration config = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Browser:BaseUrl"] = "https://test.example.invalid",
        });

        BrowserOptions options = OptionsValidation.BindAndValidate<BrowserOptions>(config, BrowserOptions.SectionName);

        Assert.That(options.IsPlaceholder(), Is.True);
    }

    [Test]
    public void BindAndValidate_RejectsInvalidUrl()
    {
        IConfiguration config = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Browser:BaseUrl"] = "not-a-url",
        });

        Assert.Throws<ConfigurationValidationException>(
            () => OptionsValidation.BindAndValidate<BrowserOptions>(config, BrowserOptions.SectionName));
    }

    [Test]
    public void BindAndValidate_RejectsNonTestEnvironmentName()
    {
        IConfiguration config = BuildConfiguration(new Dictionary<string, string?>
        {
            ["TestEnvironment:Name"] = "Production",
        });

        Assert.Throws<ConfigurationValidationException>(
            () => OptionsValidation.BindAndValidate<TestEnvironmentOptions>(config, TestEnvironmentOptions.SectionName));
    }

    [Test]
    public void BindAndValidate_RejectsOutOfRangeTimeout()
    {
        IConfiguration config = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Api:BaseUrl"] = "https://api.test.example.invalid",
            ["Api:TimeoutMs"] = "10",
        });

        Assert.Throws<ConfigurationValidationException>(
            () => OptionsValidation.BindAndValidate<ApiOptions>(config, ApiOptions.SectionName));
    }

    [Test]
    public void ApiOptions_IsPlaceholder_WhenTokenMissing()
    {
        var options = new ApiOptions { BaseUrl = "https://api.real.example.com" };

        Assert.That(options.IsPlaceholder(), Is.True);
    }
}
