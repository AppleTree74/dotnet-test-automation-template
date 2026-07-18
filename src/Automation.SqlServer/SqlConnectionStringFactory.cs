using Automation.Core.Configuration;
using Automation.Core.Exceptions;
using Microsoft.Data.SqlClient;

namespace Automation.SqlServer;

/// <summary>
/// Builds the effective connection string, applying <c>ApplicationIntent=ReadOnly</c> where
/// configured. This is routing intent, not authorization; database permissions are authoritative
/// (guide section 10.2).
/// </summary>
public static class SqlConnectionStringFactory
{
    public static string Build(SqlServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new NotConfiguredException(
                "SqlServer:ConnectionString is not configured. Supply it via user-secrets or a GitHub Environment secret.");
        }

        var builder = new SqlConnectionStringBuilder(options.ConnectionString);
        if (options.ApplyReadOnlyIntent && builder.ApplicationIntent != ApplicationIntent.ReadOnly)
        {
            builder.ApplicationIntent = ApplicationIntent.ReadOnly;
        }

        return builder.ConnectionString;
    }
}
