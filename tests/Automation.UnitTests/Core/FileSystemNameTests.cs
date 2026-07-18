using Automation.Core.Identity;
using NUnit.Framework;

namespace Automation.UnitTests.Core;

[TestFixture]
public sealed class FileSystemNameTests
{
    [Test]
    public void Sanitize_ProducesFilesystemSafeSegment()
    {
        string result = FileSystemName.Sanitize("Application.Tests.UI.LoginTests.CustomerCanSignIn(Chromium)");

        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Not.Contain(" "));
            Assert.That(result, Does.Not.Contain("/"));
            Assert.That(result, Does.Not.Contain("\\"));
            Assert.That(result, Does.Match("^[a-z0-9-]+$"));
        });
    }

    [Test]
    public void Sanitize_IsStableForSameInput()
    {
        const string Name = "Application.Tests.API.OrdersTests.CanListOrders";

        Assert.That(FileSystemName.Sanitize(Name), Is.EqualTo(FileSystemName.Sanitize(Name)));
    }

    [Test]
    public void Sanitize_DisambiguatesNamesThatCollapseToSameText()
    {
        string a = FileSystemName.Sanitize("Orders.Test/One");
        string b = FileSystemName.Sanitize("Orders.Test\\One");

        // Same sanitized text, but the appended hash keeps them distinct.
        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Sanitize_BoundsLength()
    {
        string result = FileSystemName.Sanitize(new string('x', 500));

        Assert.That(result.Length, Is.LessThanOrEqualTo(120));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Sanitize_RejectsEmpty(string value)
    {
        Assert.Throws<ArgumentException>(() => FileSystemName.Sanitize(value));
    }
}
