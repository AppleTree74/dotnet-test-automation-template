#requires -Version 7.0
<#
.SYNOPSIS
    Stable, allow-listed entry point for running tests locally and in CI (guide section 12.1).

.DESCRIPTION
    Humans, Codex, Claude Code, and GitHub Actions all invoke this one script. Every enum-like
    input is validated before a filter is composed; arbitrary text is never concatenated into a
    shell command or an NUnit filter.

.EXAMPLE
    pwsh ./scripts/Invoke-Tests.ps1 -Type UI -Suite Smoke -Browser chromium -Workers 4

.EXAMPLE
    pwsh ./scripts/Invoke-Tests.ps1 -TestName Page_RendersContent_AndLocatorsResolve
#>
[CmdletBinding()]
param(
    [ValidateSet('UI', 'API', 'Database', 'E2E', 'All')]
    [string]$Type = 'All',

    [ValidateSet('Smoke', 'Regression', 'All')]
    [string]$Suite = 'Smoke',

    [ValidateSet('chromium', 'firefox', 'webkit', 'all')]
    [string]$Browser = 'chromium',

    [ValidateSet('1', '2', '4', '8')]
    [string]$Workers = '4',

    [string]$Tags = '',

    [string]$TestName = '',

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Use the classic console logger so results appear in redirected/CI logs, not only a live terminal.
$env:MSBUILDTERMINALLOGGER = 'off'

$repoRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $repoRoot 'tests/Application.Tests/Application.Tests.csproj'

# Sentinel browser label for browser-free (API/Database) runs. TestRun records it verbatim in the
# manifest (it is not a parseable BrowserKind, so it surfaces as "not-applicable").
$NotApplicableBrowser = 'not-applicable'

function New-NUnitFilter {
    param([string]$TypeClause, [string]$Suite, [string]$Tags, [string]$TestName)

    if ($TestName) {
        if ($TestName -notmatch '^[A-Za-z0-9_.]+$') {
            throw "TestName '$TestName' contains unsupported characters. Allowed: letters, digits, '.', '_'."
        }
        return "FullyQualifiedName~$TestName"
    }

    $clauses = @()
    if ($TypeClause) { $clauses += $TypeClause }
    if ($Suite -ne 'All') { $clauses += "TestCategory=$Suite" }

    if ($Tags) {
        foreach ($tag in ($Tags -split ',')) {
            $trimmed = $tag.Trim()
            if (-not $trimmed) { continue }
            if ($trimmed -notmatch '^[A-Za-z0-9:_-]+$') {
                throw "Tag '$trimmed' is not an allow-listed category token."
            }
            $clauses += "TestCategory=$trimmed"
        }
    }

    return ($clauses -join '&')
}

# Builds an NUnit type clause for one or more test types, e.g. @('API','Database') ->
# "(TestCategory=API|TestCategory=Database)". An empty set means "no type restriction".
function Get-TypeClause {
    param([string[]]$Types)

    if (-not $Types -or $Types.Count -eq 0) { return '' }
    $parts = @($Types | ForEach-Object { "TestCategory=$_" })
    if ($parts.Count -eq 1) { return $parts[0] }
    return '(' + ($parts -join '|') + ')'
}

# Composes the set of runs for the selected Type/Browser. Browser-using types (UI, E2E) expand
# across the browser matrix; browser-free types (API, Database) run exactly once regardless of
# -Browser, so `-Browser all` no longer repeats them (P2-04). Each run is a hashtable with the
# target browser label and the NUnit type clause to execute.
function Get-Runs {
    if ($TestName) {
        # A targeted run selects by fully-qualified name; its type/suite are not category-derived.
        # Browser matters only if the named test is UI/E2E, which is not known here, so keep the
        # selected browser (never fan out to the whole matrix for a single test).
        $browser = if ($Browser -eq 'all') { 'chromium' } else { $Browser }
        return @(@{ Browser = $browser; TypeClause = '' })
    }

    $matrix = if ($Browser -eq 'all') { @('chromium', 'firefox', 'webkit') } else { @($Browser) }

    switch ($Type) {
        'API' { return @(@{ Browser = $NotApplicableBrowser; TypeClause = (Get-TypeClause @('API')) }) }
        'Database' { return @(@{ Browser = $NotApplicableBrowser; TypeClause = (Get-TypeClause @('Database')) }) }
        'UI' { return @($matrix | ForEach-Object { @{ Browser = $_; TypeClause = (Get-TypeClause @('UI')) } }) }
        'E2E' { return @($matrix | ForEach-Object { @{ Browser = $_; TypeClause = (Get-TypeClause @('E2E')) } }) }
        default {
            # Type = All.
            if ($Browser -ne 'all') {
                # One mixed run: API/Database execute once; UI/E2E use the single selected browser.
                return @(@{ Browser = $Browser; TypeClause = '' })
            }
            # Browser = all: run API/Database once (browser-free), then UI/E2E per browser, so the
            # browser-free suites are not repeated three times.
            $runs = @(@{ Browser = $NotApplicableBrowser; TypeClause = (Get-TypeClause @('API', 'Database')) })
            $runs += @($matrix | ForEach-Object { @{ Browser = $_; TypeClause = (Get-TypeClause @('UI', 'E2E')) } })
            return $runs
        }
    }
}

function Invoke-Run {
    param([string]$BrowserName, [string]$TypeClause)

    $runId = ('{0}Z-{1}' -f (Get-Date -AsUTC -Format 'yyyyMMddTHHmmss'), (Get-Random -Minimum 1000000 -Maximum 9999999))
    $runRoot = Join-Path $repoRoot (Join-Path 'artifacts' $runId)
    New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

    $filter = New-NUnitFilter -TypeClause $TypeClause -Suite $Suite -Tags $Tags -TestName $TestName

    $env:AUTOMATION_RUN_ID = $runId
    $env:AUTOMATION_TYPE = $Type.ToLowerInvariant()
    $env:AUTOMATION_SUITE = $Suite.ToLowerInvariant()
    $env:AUTOMATION_BROWSER = $BrowserName
    $env:AUTOMATION_WORKERS = $Workers
    # Record targeted selection so the manifest is not mislabelled with the default type/suite (P3-03).
    if ($TestName) {
        $env:AUTOMATION_TEST_NAME = $TestName
    }
    else {
        Remove-Item Env:AUTOMATION_TEST_NAME -ErrorAction SilentlyContinue
    }

    Write-Host "== Run $runId | type=$Type suite=$Suite browser=$BrowserName workers=$Workers ==" -ForegroundColor Cyan
    if ($filter) { Write-Host "   filter: $filter" -ForegroundColor DarkGray }

    $arguments = @(
        'test', $testProject,
        '-c', $Configuration,
        '--results-directory', $runRoot,
        '--logger', 'trx;LogFileName=test-results.trx'
    )
    if ($filter) { $arguments += @('--filter', $filter) }
    # Runsettings passed after '--' control NUnit's worker count.
    $arguments += @('--', "NUnit.NumberOfTestWorkers=$Workers")

    # Pipe dotnet's output to the host so it stays visible in terminals and CI logs but does NOT
    # become part of this function's return value. Returning the exit code as the only pipeline
    # output keeps the caller's $code a single integer, so a failing run propagates a non-zero
    # process exit code (the original test status is authoritative).
    & dotnet @arguments | Out-Host
    $exitCode = $LASTEXITCODE

    Write-Host "== Run $runId finished with exit code $exitCode ==" -ForegroundColor Cyan
    return [int]$exitCode
}

$runs = Get-Runs

# Clean Allure results once before the (possibly multi-run) launch, then let every run's results
# accumulate into the one report. TestRun honours AUTOMATION_KEEP_ALLURE_RESULTS and does not
# re-clean per launch, so a `-Browser all` run keeps chromium, firefox, and webkit results.
$allureResults = Join-Path $repoRoot 'allure-results'
if (Test-Path $allureResults) { Remove-Item -Recurse -Force $allureResults }
$env:AUTOMATION_KEEP_ALLURE_RESULTS = '1'

$overall = 0
foreach ($run in $runs) {
    $code = Invoke-Run -BrowserName $run.Browser -TypeClause $run.TypeClause
    if ($code -ne 0) { $overall = $code }
}

# The original test exit status is authoritative; reporting must not convert failure to pass.
exit $overall
