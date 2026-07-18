using System.Diagnostics;
using Automation.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Application.Tests.Framework;

/// <summary>
/// Base for Database tests. Exposes the read-only SQL client and records safe query evidence.
/// The client is resolved lazily, so an unconfigured template only fails when a Database test
/// actually runs (such tests are skipped until a connection string is supplied).
/// </summary>
public abstract class DatabaseTestBase : AutomationTestBase
{
    private readonly List<SqlEvidence> _evidence = [];

    protected IReadOnlySqlClient Sql => Services.GetRequiredService<IReadOnlySqlClient>();

    /// <summary>Runs a query and records safe evidence (query id, elapsed, row count, parameter names).</summary>
    protected async Task<IReadOnlyList<T>> QueryAsync<T>(SqlQuery query, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            IReadOnlyList<T> rows = await Sql.QueryAsync<T>(query, cancellationToken);
            stopwatch.Stop();
            _evidence.Add(new SqlEvidence
            {
                QueryId = query.QueryId,
                ElapsedMs = stopwatch.Elapsed.TotalMilliseconds,
                RowCount = rows.Count,
                ParameterNames = query.ParameterNames,
            });
            return rows;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _evidence.Add(new SqlEvidence
            {
                QueryId = query.QueryId,
                ElapsedMs = stopwatch.Elapsed.TotalMilliseconds,
                ParameterNames = query.ParameterNames,
                Error = ex.GetType().Name,
            });
            throw;
        }
    }

    [TearDown]
    public async Task DatabaseTearDown()
    {
        if (_evidence.Count > 0)
        {
            await SqlEvidenceWriter.WriteAsync(TestArtifactDirectory, _evidence);
        }
    }
}
