#requires -Version 7.0
<#
.SYNOPSIS
    Installs Playwright browser binaries for the built test project.

.DESCRIPTION
    Builds Application.Tests (so the generated playwright.ps1 exists) and installs only the
    requested browsers. API- and Database-only runs never need this (guide section 13.2).

.EXAMPLE
    pwsh ./scripts/Install-Playwright.ps1 -Browser chromium

.EXAMPLE
    pwsh ./scripts/Install-Playwright.ps1 -Browser all -WithDeps   # Linux CI
#>
[CmdletBinding()]
param(
    [ValidateSet('chromium', 'firefox', 'webkit', 'all')]
    [string]$Browser = 'chromium',

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$WithDeps
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $repoRoot 'tests/Application.Tests/Application.Tests.csproj'
$targetFramework = 'net10.0'

Write-Host "Building test project so Playwright tooling is available..." -ForegroundColor Cyan
& dotnet build $testProject -c $Configuration | Out-Host
if ($LASTEXITCODE -ne 0) { throw "Build failed; cannot install Playwright browsers." }

$playwrightScript = Join-Path $repoRoot "tests/Application.Tests/bin/$Configuration/$targetFramework/playwright.ps1"
if (-not (Test-Path $playwrightScript)) {
    throw "playwright.ps1 not found at $playwrightScript. Ensure Microsoft.Playwright is referenced."
}

$browserArgs = if ($Browser -eq 'all') { @('chromium', 'firefox', 'webkit') } else { @($Browser) }
$installArgs = @('install') + $browserArgs
if ($WithDeps) { $installArgs += '--with-deps' }

Write-Host "Installing Playwright browsers: $($browserArgs -join ', ')" -ForegroundColor Cyan
& $playwrightScript @installArgs
if ($LASTEXITCODE -ne 0) { throw "Playwright browser installation failed." }

Write-Host "Playwright browsers installed." -ForegroundColor Green
