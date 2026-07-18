namespace Automation.Core.Identity;

/// <summary>The single required test type. Every test has exactly one (guide section 12).</summary>
public enum TestType
{
    UI,
    API,
    Database,
    E2E,
}

/// <summary>Execution suite. Every test belongs to at least one.</summary>
public enum Suite
{
    Smoke,
    Regression,
}

/// <summary>
/// Immutable identity and classification for one executing test. Carries enough context to
/// map every artifact and structured event back to one run and one test (guide section 7.1).
/// </summary>
public sealed record TestIdentity
{
    public required string TestId { get; init; }

    public required string FullyQualifiedName { get; init; }

    public required TestType Type { get; init; }

    public required IReadOnlyList<Suite> Suites { get; init; }

    /// <summary>Browser under test, or <c>not-applicable</c> for API/Database tests.</summary>
    public string Browser { get; init; } = NotApplicable;

    public int Worker { get; init; }

    public const string NotApplicable = "not-applicable";

    /// <summary>
    /// Builds a test identity from the fully qualified NUnit test name. The resulting
    /// <see cref="TestId"/> is filesystem-safe and stable for the same name.
    /// </summary>
    public static TestIdentity Create(
        string fullyQualifiedName,
        TestType type,
        IReadOnlyList<Suite> suites,
        string? browser = null,
        int worker = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullyQualifiedName);
        ArgumentNullException.ThrowIfNull(suites);
        if (suites.Count == 0)
        {
            throw new ArgumentException("A test must belong to at least one suite.", nameof(suites));
        }

        return new TestIdentity
        {
            TestId = FileSystemName.Sanitize(fullyQualifiedName),
            FullyQualifiedName = fullyQualifiedName,
            Type = type,
            Suites = suites,
            Browser = string.IsNullOrWhiteSpace(browser) ? NotApplicable : browser,
            Worker = worker,
        };
    }
}
