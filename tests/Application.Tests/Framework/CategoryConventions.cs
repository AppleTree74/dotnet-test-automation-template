using Automation.Core.Identity;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Application.Tests.Framework;

/// <summary>Category naming conventions and the one-Type/at-least-one-Suite invariant (guide section 12).</summary>
public static class CategoryConventions
{
    public const string TypeProperty = "AutomationType";
    public const string SuiteProperty = "AutomationSuite";
    public const string FeaturePrefix = "Feature:";

    private static readonly HashSet<string> TypeNames =
        new(Enum.GetNames<TestType>(), StringComparer.Ordinal);

    private static readonly HashSet<string> SuiteNames =
        new(Enum.GetNames<Suite>(), StringComparer.Ordinal);

    /// <summary>
    /// Validates that the current test declares exactly one type and at least one suite. Categories
    /// declared on the fixture are inherited, so ancestors are included. Throws
    /// <see cref="InvalidOperationException"/> otherwise so a mis-tagged test fails loudly.
    /// </summary>
    public static (TestType Type, IReadOnlyList<Suite> Suites) ResolveAndValidate(ITest test)
    {
        ArgumentNullException.ThrowIfNull(test);

        var categories = new List<string>();
        for (ITest? current = test; current is not null; current = current.Parent)
        {
			if (current.Properties.ContainsKey(PropertyNames.Category))
			{
				categories.AddRange(current.Properties[PropertyNames.Category].Cast<string>());
			}
        }

        List<TestType> types = categories
            .Where(TypeNames.Contains)
            .Select(Enum.Parse<TestType>)
            .Distinct()
            .ToList();

        List<Suite> suites = categories
            .Where(SuiteNames.Contains)
            .Select(Enum.Parse<Suite>)
            .Distinct()
            .ToList();

        if (types.Count != 1)
        {
            throw new InvalidOperationException(
                $"Test '{test.Name}' must declare exactly one type (UI/API/Database/E2E) but declared {types.Count}. " +
                "Use [TestType(...)].");
        }

        if (suites.Count == 0)
        {
            throw new InvalidOperationException(
                $"Test '{test.Name}' must declare at least one suite (Smoke/Regression). Use [Suite(...)].");
        }

        return (types[0], suites);
    }
}
