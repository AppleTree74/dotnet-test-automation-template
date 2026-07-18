using System.ComponentModel.DataAnnotations;

namespace Automation.Core.Configuration;

/// <summary>
/// Root configuration for the single supported environment. The framework runs against
/// <c>Test</c> only; there is deliberately no environment selector (see
/// AI_IMPLEMENTATION_GUIDE.md section 3). URLs and secrets vary per generated application.
/// </summary>
public sealed class TestEnvironmentOptions
{
    public const string SectionName = "TestEnvironment";

    /// <summary>The only permitted environment name.</summary>
    public const string RequiredName = "Test";

    [Required]
    [RegularExpression(RequiredName, ErrorMessage = "Only the 'Test' environment is supported.")]
    public string Name { get; init; } = RequiredName;
}
