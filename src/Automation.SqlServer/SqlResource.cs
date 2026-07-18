using System.Reflection;

namespace Automation.SqlServer;

/// <summary>
/// Loads reviewed <c>.sql</c> query text from embedded resources. Product SQL lives in
/// <c>Application.Automation/Database/Queries</c> as embedded resources (guide section 10.2, step 4).
/// </summary>
public static class SqlResource
{
    /// <summary>Reads embedded SQL by its resource name suffix (e.g. <c>Orders.GetById.sql</c>).</summary>
    public static string Load(Assembly assembly, string resourceNameSuffix)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceNameSuffix);

        string normalized = resourceNameSuffix.Replace('/', '.').Replace('\\', '.');
        string? fullName = Array.Find(
            assembly.GetManifestResourceNames(),
            name => name.EndsWith(normalized, StringComparison.OrdinalIgnoreCase));

        if (fullName is null)
        {
            throw new FileNotFoundException($"Embedded SQL resource ending in '{normalized}' was not found in {assembly.GetName().Name}.");
        }

        using Stream stream = assembly.GetManifestResourceStream(fullName)
            ?? throw new FileNotFoundException($"Embedded SQL resource '{fullName}' could not be opened.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>Loads embedded SQL and wraps it in a validated <see cref="SqlQuery"/>.</summary>
    public static SqlQuery LoadQuery(Assembly assembly, string queryId, string resourceNameSuffix)
    {
        string sql = Load(assembly, resourceNameSuffix);
        SqlCommandValidator.Validate(sql);
        return new SqlQuery(queryId, sql);
    }
}
