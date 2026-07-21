#requires -Version 7.0
<#
.SYNOPSIS
    Generates the Allure Report 3 HTML report and updates durable history.

.DESCRIPTION
    Restores Node dependencies with `npm ci` when needed, then runs `allure generate` using
    allurerc.mjs. The single-file history at allure-history/history.jsonl is updated in place; CI
    restores it from a durable store beforehand and persists it afterwards (guide section 14).
    Reporting never changes the authoritative test exit status.

.EXAMPLE
    pwsh ./scripts/Generate-Allure.ps1
#>
[CmdletBinding()]
param(
    [string]$ResultsDirectory = 'allure-results'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    if (-not (Test-Path (Join-Path $repoRoot $ResultsDirectory))) {
        Write-Warning "No results directory '$ResultsDirectory'; nothing to generate."
        return
    }

    if (-not (Test-Path (Join-Path $repoRoot 'node_modules'))) {
        Write-Host "Installing Node dependencies (npm ci)..." -ForegroundColor Cyan
        & npm ci | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "npm ci failed." }
    }

    New-Item -ItemType Directory -Force -Path (Join-Path $repoRoot 'allure-history') | Out-Null

    # Redact a publication copy of the results before generation (P1-3). The report and its history
    # are built from the sanitized copy so free-text the attachment policy never sees — Allure
    # statusDetails (Playwright/NUnit failure text quoting the DOM), parameters, labels, and step
    # names — cannot carry secrets to Pages. The raw results stay untouched for workflow diagnostics.
    # Sanitization fails closed: a parse error aborts here rather than publishing raw data.
    $sanitizedDirectory = Join-Path $repoRoot 'allure-results-sanitized'
    if (Test-Path $sanitizedDirectory) {
        Remove-Item -Recurse -Force $sanitizedDirectory
    }

    Write-Host "Sanitizing Allure results for publication..." -ForegroundColor Cyan
    $sanitizer = Join-Path $repoRoot 'tools/AllureResultsSanitizer/AllureResultsSanitizer.csproj'
    & dotnet run --project $sanitizer -c Release -- (Join-Path $repoRoot $ResultsDirectory) $sanitizedDirectory | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Allure results sanitization failed; report not generated." }

    # Remove any previous report first. Allure writes into allure-report/ (with the awesome plugin
    # under allure-report/awesome/); without this, a stale root index.html from an earlier run can
    # survive and be served in place of the current report. CI runners normally start clean, so this
    # primarily protects local and persistent self-hosted runs.
    $reportDirectory = Join-Path $repoRoot 'allure-report'
    if (Test-Path $reportDirectory) {
        Remove-Item -Recurse -Force $reportDirectory
    }

    Write-Host "Generating Allure Report 3 from the sanitized results..." -ForegroundColor Cyan
    & npx allure generate $sanitizedDirectory | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "allure generate failed." }

    Write-Host "Report written to allure-report/; history updated at allure-history/history.jsonl." -ForegroundColor Green
}
finally {
    Pop-Location
}
