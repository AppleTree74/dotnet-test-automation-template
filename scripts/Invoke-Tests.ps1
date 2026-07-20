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

function New-NUnitFilter {
    param([string]$Type, [string]$Suite, [string]$Tags, [string]$TestName)

    if ($TestName) {
        if ($TestName -notmatch '^[A-Za-z0-9_.]+$') {
            throw "TestName '$TestName' contains unsupported characters. Allowed: letters, digits, '.', '_'."
        }
        return "FullyQualifiedName~$TestName"
    }

    $clauses = @()
    if ($Type -ne 'All') { $clauses += "TestCategory=$Type" }
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

function Invoke-SingleBrowser {
    param([string]$BrowserName)

    $runId = ('{0}Z-{1}' -f (Get-Date -AsUTC -Format 'yyyyMMddTHHmmss'), (Get-Random -Minimum 1000000 -Maximum 9999999))
    $runRoot = Join-Path $repoRoot (Join-Path 'artifacts' $runId)
    New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

    $filter = New-NUnitFilter -Type $Type -Suite $Suite -Tags $Tags -TestName $TestName

    $env:AUTOMATION_RUN_ID = $runId
    $env:AUTOMATION_TYPE = $Type.ToLowerInvariant()
    $env:AUTOMATION_SUITE = $Suite.ToLowerInvariant()
    $env:AUTOMATION_BROWSER = $BrowserName
    $env:AUTOMATION_WORKERS = $Workers

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

$browsers = if ($Browser -eq 'all') { @('chromium', 'firefox', 'webkit') } else { @($Browser) }

# Clean Allure results once before the (possibly multi-browser) launch, then let every browser's
# results accumulate into the one report. TestRun honours AUTOMATION_KEEP_ALLURE_RESULTS and does
# not re-clean per launch, so a `-Browser all` run keeps chromium, firefox, and webkit results.
$allureResults = Join-Path $repoRoot 'allure-results'
if (Test-Path $allureResults) { Remove-Item -Recurse -Force $allureResults }
$env:AUTOMATION_KEEP_ALLURE_RESULTS = '1'

$overall = 0
foreach ($b in $browsers) {
    $code = Invoke-SingleBrowser -BrowserName $b
    if ($code -ne 0) { $overall = $code }
}

# The original test exit status is authoritative; reporting must not convert failure to pass.
exit $overall
