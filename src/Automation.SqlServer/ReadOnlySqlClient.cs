using System.Diagnostics;
using Automation.Core.Configuration;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Automation.SqlServer;

/// <summary>
/// Default <see cref="IReadOnlySqlClient"/>. Validates every command as read-only, binds named
/// parameters, and keeps <see cref="SqlConnection"/> and Dapper strictly internal. Emits only safe
/// evidence (query id, elapsed time, row count, parameter names).
/// </summary>
public sealed class ReadOnlySqlClient : IReadOnlySqlClient
{
    private readonly SqlServerOptions _options;
    private readonly ILogger<ReadOnlySqlClient> _logger;
    private readonly string _connectionString;

    public ReadOnlySqlClient(SqlServerOptions options, ILogger<ReadOnlySqlClient> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionString = SqlConnectionStringFactory.Build(options);
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(SqlQuery query, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            query,
            async (connection, command) => (await connection.QueryAsync<T>(command).ConfigureAwait(false)).ToList(),
            result => result.Count,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(SqlQuery query, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            query,
            (connection, command) => connection.QuerySingleOrDefaultAsync<T>(command),
            result => result is null ? 0 : 1,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<T?> ScalarAsync<T>(SqlQuery query, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            query,
            (connection, command) => connection.ExecuteScalarAsync<T>(command),
            result => result is null ? 0 : 1,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResult> ExecuteAsync<TResult>(
        SqlQuery query,
        Func<SqlConnection, CommandDefinition, Task<TResult>> execute,
        Func<TResult, int> rowCount,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Defense in depth: validate before the command reaches the database.
        SqlCommandValidator.Validate(query.Sql);

        var parameters = new DynamicParameters();
        foreach (KeyValuePair<string, object?> parameter in query.Parameters)
        {
            parameters.Add(parameter.Key, parameter.Value);
        }

        await using var connection = new SqlConnection(_connectionString);
        var command = new CommandDefinition(
            query.Sql,
            parameters,
            commandTimeout: _options.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        TResult result = await execute(connection, command).ConfigureAwait(false);
        stopwatch.Stop();

        _logger.LogInformation(
            "SQL {QueryId} returned {Rows} row(s) in {Elapsed:F0} ms; parameters: [{Params}].",
            query.QueryId,
            rowCount(result),
            stopwatch.Elapsed.TotalMilliseconds,
            string.Join(", ", query.ParameterNames));

        return result;
    }
}
