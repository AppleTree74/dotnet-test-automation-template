using System.Reflection;
using Automation.SqlServer;

namespace Application.Automation.Database;

/// <summary>Builds validated read-only queries from reviewed embedded <c>.sql</c> resources.</summary>
public static class CustomerQueries
{
    private static readonly Assembly ResourceAssembly = typeof(CustomerQueries).Assembly;

    public static SqlQuery GetById(string customerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        return SqlResource
            .LoadQuery(ResourceAssembly, "Customers.GetById", "Customers.GetById.sql")
            .WithParameter("id", customerId);
    }
}
