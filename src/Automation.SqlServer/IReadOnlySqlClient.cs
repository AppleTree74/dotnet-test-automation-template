namespace Automation.SqlServer;

/// <summary>
/// The entire public SQL surface (guide section 10.1): parameterized, read-only queries. There
/// is deliberately no DML, no schema API, no stored-procedure execution, and no raw
/// <c>SqlConnection</c>. Database access verifies state; it never creates, repairs, or removes it.
/// </summary>
public interface IReadOnlySqlClient
{
    Task<IReadOnlyList<T>> QueryAsync<T>(SqlQuery query, CancellationToken cancellationToken = default);

    Task<T?> QuerySingleOrDefaultAsync<T>(SqlQuery query, CancellationToken cancellationToken = default);

    Task<T?> ScalarAsync<T>(SqlQuery query, CancellationToken cancellationToken = default);
}
