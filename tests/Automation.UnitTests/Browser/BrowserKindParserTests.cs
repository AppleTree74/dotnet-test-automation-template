using Automation.Browser;
using Automation.Core.Configuration;
using NUnit.Framework;

namespace Automation.UnitTests.Browser;

[TestFixture]
public sealed class BrowserKindParserTests
{
    [TestCase("chromium", BrowserKind.Chromium)]
    [TestCase("Firefox", BrowserKind.Firefox)]
    [TestCase("  webkit ", BrowserKind.Webkit)]
    public void Parse_AcceptsAllowListedNames(string value, BrowserKind expected)
    {
        Assert.That(BrowserKindParser.Parse(value), Is.EqualTo(expected));
    }

    [TestCase("internet-explorer")]
    [TestCase("chromium; rm -rf")]
    [TestCase("")]
    [TestCase(null)]
    public void Parse_RejectsUnknownNames(string? value)
    {
        Assert.That(BrowserKindParser.TryParse(value, out _), Is.False);
        Assert.Throws<ArgumentException>(() => BrowserKindParser.Parse(value));
    }
}
