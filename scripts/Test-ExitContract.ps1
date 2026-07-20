#requires -Version 7.0
<#
.SYNOPSIS
    Regression test for the authoritative-exit-status contract (guide: the original test exit
    status remains authoritative).

.DESCRIPTION
    Runs two browser-free probe tests through scripts/Invoke-Tests.ps1 and asserts that a passing
    test yields process exit 0 and a failing test yields a non-zero exit. Guards against the
    PowerShell output-capture regression where a failing run could return exit 0. Runs in CI
    (validate.yml); needs no browser or Test environment.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$invoke = Join-Path $PSScriptRoot 'Invoke-Tests.ps1'
$failures = 0

function Assert-ExitCode {
    param([string]$TestName, [ValidateSet('zero', 'nonzero')] [string]$Expected)

    & pwsh -NoProfile -File $invoke -TestName $TestName -Configuration $Configuration | Out-Host
    $code = $LASTEXITCODE

    $ok = if ($Expected -eq 'zero') { $code -eq 0 } else { $code -ne 0 }
    if ($ok) {
        Write-Host "PASS: '$TestName' exited $code (expected $Expected)." -ForegroundColor Green
    }
    else {
        Write-Host "FAIL: '$TestName' exited $code (expected $Expected)." -ForegroundColor Red
        $script:failures++
    }
}

Push-Location $repoRoot
try {
    Assert-ExitCode -TestName 'ExitContract_Probe_Passes' -Expected 'zero'
    Assert-ExitCode -TestName 'ExitContract_Probe_Fails' -Expected 'nonzero'
}
finally {
    Pop-Location
}

if ($failures -gt 0) {
    Write-Host "Exit-contract regression FAILED ($failures)." -ForegroundColor Red
    exit 1
}

Write-Host "Exit-contract regression passed." -ForegroundColor Green
