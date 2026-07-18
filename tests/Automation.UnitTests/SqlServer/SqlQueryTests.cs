using Automation.Core.Configuration;
using Automation.Core.Exceptions;
using Automation.SqlServer;
using NUnit.Framework;

namespace Automation.UnitTests.SqlServer;

[TestFixture]
public sealed class SqlQueryTests
{
    [Test]
    public void WithParameter_NormalizesLeadingAtSign()
    {
        var query = new SqlQuery("Orders.GetById", "SELECT * FROM Orders WHERE Id = @id")
            .WithParameter("@id", 42)
            .WithParameter("status", "active");

        Assert.Multiple(() =>
        {
            Assert.That(query.Parameters.ContainsKey("id"), Is.True);
            Assert.That(query.Parameters.ContainsKey("status"), Is.True);
            Assert.That(query.Parameters["id"], Is.EqualTo(42));
            Assert.That(query.ParameterNames, Is.EquivalentTo(new[] { "id", "status" }));
        });
    }

    [Test]
    public void Constructor_RejectsEmptySqlOrId()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => new SqlQuery("id", ""));
            Assert.Throws<ArgumentException>(() => new SqlQuery("", "SELECT 1"));
        });
    }

    [Test]
    public void ConnectionStringFactory_AppliesReadOnlyIntent()
    {
        var options = new SqlServerOptions
        {
            ConnectionString = "Server=db;Database=app;User ID=svc;Password=secret;Encrypt=True;TrustServerCertificate=True",
            ApplyReadOnlyIntent = true,
        };

        string result = SqlConnectionStringFactory.Build(options);

        Assert.That(result, Does.Contain("ReadOnly"));
    }

    [Test]
    public void ConnectionStringFactory_ThrowsWhenNotConfigured()
    {
        var options = new SqlServerOptions { ConnectionString = null };

        Assert.Throws<NotConfiguredException>(() => SqlConnectionStringFactory.Build(options));
    }
}
