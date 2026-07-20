#requires -Version 7.0
<#
.SYNOPSIS
    Generates a repository from the template and validates it end to end.

.DESCRIPTION
    Installs the template from this repository, generates a solution into a temporary directory,
    and runs locked restore, Release build, format verification, framework unit tests, and the
    generated-repository validator against it. Guards against P1-01 and other
    template-generation regressions. Needs no browser or Test environment. Cleans up on exit.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$work = Join-Path ([System.IO.Path]::GetTempPath()) ("tmpl-gen-" + [guid]::NewGuid().ToString('N'))
$generated = Join-Path $work 'Generated.Product'

function Invoke-Step {
    param([string]$Name, [scriptblock]$Action)
    Write-Host "== $Name ==" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) { throw "$Name failed (exit $LASTEXITCODE)." }
}

try {
    Invoke-Step 'Install template' { dotnet new install $repoRoot --force | Out-Host }
    Invoke-Step 'Generate solution' { dotnet new test-automation -n Generated.Product -o $generated | Out-Host }

    Push-Location $generated
    try {
        $solution = (Get-ChildItem -Filter '*.slnx' -File | Select-Object -First 1).Name
        if (-not $solution) { throw "Generated repository has no .slnx solution file." }
        Write-Host "Generated solution: $solution" -ForegroundColor DarkGray

        Invoke-Step 'Locked restore' { dotnet restore $solution --locked-mode | Out-Host }
        Invoke-Step 'Release build' { dotnet build $solution -c $Configuration --no-restore | Out-Host }
        Invoke-Step 'Format verify' { dotnet format $solution --verify-no-changes --no-restore | Out-Host }
        Invoke-Step 'Framework unit tests' { dotnet test tests/Automation.UnitTests/Automation.UnitTests.csproj -c $Configuration --no-build | Out-Host }
        Invoke-Step 'Generated-repository validation' { pwsh -NoProfile -File ./scripts/Validate-Template.ps1 | Out-Host }
    }
    finally {
        Pop-Location
    }

    Write-Host "Template generation validation passed." -ForegroundColor Green
}
finally {
    dotnet new uninstall $repoRoot 2>$null | Out-Null
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}
