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

    Write-Host "Generating Allure Report 3 from '$ResultsDirectory'..." -ForegroundColor Cyan
    & npx allure generate $ResultsDirectory | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "allure generate failed." }

    Write-Host "Report written to allure-report/; history updated at allure-history/history.jsonl." -ForegroundColor Green
}
finally {
    Pop-Location
}
