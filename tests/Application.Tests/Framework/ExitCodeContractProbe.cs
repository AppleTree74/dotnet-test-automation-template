using Allure.NUnit;
using Automation.Core.Identity;
using NUnit.Framework;

namespace Application.Tests.Framework;

/// <summary>
/// Deterministic, browser-free probes used by <c>scripts/Test-ExitContract.ps1</c> to guard the
/// authoritative-exit-status contract (a failing test MUST make <c>Invoke-Tests.ps1</c> exit
/// non-zero; a passing one MUST exit zero). Both are <see cref="ExplicitAttribute"/> so they never
/// run in normal suites — they are selected by name only.
/// </summary>
[TestFixture]
[AllureNUnit]
[TestType(TestType.API)]
[Suite(Suite.Regression)]
[Feature("Framework")]
[Explicit("Exit-code contract probes; selected by name from scripts/Test-ExitContract.ps1.")]
public sealed class ExitCodeContractProbe : ApiTestBase
{
    [Test]
    public void ExitContract_Probe_Passes() => Assert.Pass();

    [Test]
    public void ExitContract_Probe_Fails() => Assert.Fail("Intentional failure for the exit-code contract test.");
}
