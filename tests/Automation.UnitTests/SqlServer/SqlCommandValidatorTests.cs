using Automation.Core.Exceptions;
using Automation.SqlServer;
using NUnit.Framework;

namespace Automation.UnitTests.SqlServer;

[TestFixture]
public sealed class SqlCommandValidatorTests
{
    [TestCase("SELECT Id, Name FROM dbo.Customers WHERE Id = @id")]
    [TestCase("select top 10 * from Orders order by CreatedUtc desc")]
    [TestCase("WITH recent AS (SELECT Id FROM Orders WHERE CreatedUtc > @since) SELECT * FROM recent")]
    [TestCase("SELECT COUNT(*) FROM dbo.Users WHERE Status = 'active' -- comment with DELETE keyword")]
    [TestCase("SELECT * FROM t WHERE Note = 'please DROP this note'")]
    public void Validate_AcceptsReadOnlyQueries(string sql)
    {
        Assert.DoesNotThrow(() => SqlCommandValidator.Validate(sql));
        Assert.That(SqlCommandValidator.IsValid(sql), Is.True);
    }

    [TestCase("INSERT INTO dbo.Customers (Name) VALUES (@name)")]
    [TestCase("UPDATE dbo.Customers SET Name = @name WHERE Id = @id")]
    [TestCase("DELETE FROM dbo.Customers WHERE Id = @id")]
    [TestCase("MERGE dbo.Target USING dbo.Source ON 1=1 WHEN MATCHED THEN DELETE")]
    [TestCase("DROP TABLE dbo.Customers")]
    [TestCase("TRUNCATE TABLE dbo.Customers")]
    [TestCase("ALTER TABLE dbo.Customers ADD Col INT")]
    [TestCase("CREATE TABLE dbo.Temp (Id INT)")]
    [TestCase("EXEC sp_who")]
    [TestCase("EXECUTE dbo.DoThing")]
    [TestCase("SELECT * INTO dbo.Copy FROM dbo.Customers")]
    [TestCase("SELECT * FROM Customers; DELETE FROM Customers")]
    [TestCase("SELECT * FROM Customers; SELECT * FROM Orders")]
    [TestCase("SELECT * FROM OPENROWSET('SQLNCLI', 'x', 'SELECT 1')")]
    [TestCase("GRANT SELECT ON dbo.Customers TO someone")]
    [TestCase("DBCC CHECKDB")]
    [TestCase("SELECT * FROM t WAITFOR DELAY '00:00:05'")]
    public void Validate_RejectsUnsafeCommands(string sql)
    {
        Assert.Throws<UnsafeSqlException>(() => SqlCommandValidator.Validate(sql));
        Assert.That(SqlCommandValidator.IsValid(sql), Is.False);
    }

    [Test]
    public void Validate_RejectsCommentHiddenBatch()
    {
        const string Sql = "SELECT 1 /* sneaky */ ; DROP TABLE dbo.Customers";

        Assert.Throws<UnsafeSqlException>(() => SqlCommandValidator.Validate(Sql));
    }

    [Test]
    public void Validate_AllowsTrailingSemicolon()
    {
        Assert.DoesNotThrow(() => SqlCommandValidator.Validate("SELECT 1;"));
    }
}
