using Application.Tests.Framework;
using NUnit.Framework;

namespace Application.Tests;

/// <summary>
/// Assembly-wide setup/teardown. A <see cref="SetUpFixtureAttribute"/> declared in the
/// <c>Application.Tests</c> root namespace runs once for every test in that namespace and all
/// nested namespaces (UI, API, Database, E2E).
/// </summary>
[SetUpFixture]
public sealed class GlobalSetup
{
    [OneTimeSetUp]
    public void BeforeAllTests() => TestRun.Initialize();

    [OneTimeTearDown]
    public async Task AfterAllTests() => await TestRun.ShutdownAsync();
}
