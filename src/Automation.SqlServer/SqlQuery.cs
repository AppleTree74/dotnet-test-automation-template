namespace Automation.SqlServer;

/// <summary>
/// A read-only query with its SQL text kept strictly separate from named parameter values
/// (guide section 10.1). Callers MUST NOT interpolate values into <see cref="Sql"/>; every value
/// travels through <see cref="Parameters"/> and is bound as a SQL parameter.
/// </summary>
public sealed class SqlQuery
{
    private readonly Dictionary<string, object?> _parameters = new(StringComparer.Ordinal);

    public SqlQuery(string queryId, string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        QueryId = queryId;
        Sql = sql;
    }

    /// <summary>Stable identifier for logging and evidence (never the raw SQL).</summary>
    public string QueryId { get; }

    /// <summary>Parameterized SQL text. Validated as read-only before execution.</summary>
    public string Sql { get; }

    public IReadOnlyDictionary<string, object?> Parameters => _parameters;

    /// <summary>Binds a named parameter value. Name may be given with or without a leading '@'.</summary>
    public SqlQuery WithParameter(string name, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string normalized = name.StartsWith('@') ? name[1..] : name;
        ArgumentException.ThrowIfNullOrWhiteSpace(normalized);
        _parameters[normalized] = value;
        return this;
    }

    /// <summary>The redacted parameter names (never values) for safe evidence.</summary>
    public IReadOnlyList<string> ParameterNames => _parameters.Keys.ToList();
}
