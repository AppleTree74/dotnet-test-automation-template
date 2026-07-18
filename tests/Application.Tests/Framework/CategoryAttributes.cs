using Automation.Core.Identity;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Application.Tests.Framework;

/// <summary>
/// Declares the single required test type (guide section 12). Applies an NUnit category equal to
/// the type name (e.g. <c>UI</c>) and records the type for validation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class TestTypeAttribute : NUnitAttribute, IApplyToTest
{
    public TestTypeAttribute(TestType type) => Type = type;

    public TestType Type { get; }

    public void ApplyToTest(Test test)
    {
        test.Properties.Add(PropertyNames.Category, Type.ToString());
        test.Properties.Set(CategoryConventions.TypeProperty, Type.ToString());
    }
}

/// <summary>Declares a suite membership (guide section 12). A test needs at least one.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class SuiteAttribute : NUnitAttribute, IApplyToTest
{
    public SuiteAttribute(Suite suite) => Suite = suite;

    public Suite Suite { get; }

    public void ApplyToTest(Test test)
    {
        test.Properties.Add(PropertyNames.Category, Suite.ToString());
        test.Properties.Add(CategoryConventions.SuiteProperty, Suite.ToString());
    }
}

/// <summary>Declares a feature category as <c>Feature:&lt;name&gt;</c>.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class FeatureAttribute : NUnitAttribute, IApplyToTest
{
    public FeatureAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    public string Name { get; }

    public void ApplyToTest(Test test) =>
        test.Properties.Add(PropertyNames.Category, $"{CategoryConventions.FeaturePrefix}{Name}");
}
