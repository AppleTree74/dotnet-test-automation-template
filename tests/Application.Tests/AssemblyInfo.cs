using NUnit.Framework;

// Tests run in parallel across fixtures. Within a fixture, tests run sequentially and rebuild
// their per-test state in SetUp/TearDown, so every test is isolated and order-independent
// (guide section 12). The worker count is overridable at run time via the NUnit runsettings
// parameter `NUnit.NumberOfTestWorkers` (set by scripts/Invoke-Tests.ps1).
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(4)]
