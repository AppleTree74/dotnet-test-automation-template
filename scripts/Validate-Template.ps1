#requires -Version 7.0
<#
.SYNOPSIS
    Structural validation for the template: required files, test taxonomy, and no committed
    secrets, absolute local paths, or leftover placeholder namespaces.

.DESCRIPTION
    Runs in CI (validate.yml) and locally. It complements the compiler and unit tests; it does not
    build. Exit code is non-zero if any check fails.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$errors = [System.Collections.Generic.List[string]]::new()

function Add-Error([string]$message) { $script:errors.Add($message) }

# 1) Required files exist.
$required = @(
    'global.json', 'Directory.Build.props', 'Directory.Packages.props',
    'AGENTS.md', 'CLAUDE.md', 'package.json', 'package-lock.json', 'allurerc.mjs',
    '.github/workflows/validate.yml', '.github/workflows/test-and-report.yml',
    'scripts/Invoke-Tests.ps1', 'scripts/Install-Playwright.ps1', 'scripts/Generate-Allure.ps1',
    'docs/architecture.md', 'docs/configuration.md', 'docs/test-standards.md', 'docs/debugging.md',
    'docs/AI_IMPLEMENTATION_GUIDE.md'
)
foreach ($rel in $required) {
    if (-not (Test-Path (Join-Path $repoRoot $rel))) { Add-Error "Missing required file: $rel" }
}

# The solution file is checked by glob because its name changes when the template is generated
# (e.g. AutomationTemplate.slnx -> Contoso.Shop.slnx).
if (-not (Get-ChildItem -Path $repoRoot -Filter '*.slnx' -File -ErrorAction SilentlyContinue)) {
    Add-Error "No .slnx solution file found."
}

# Template-authoring metadata exists only in the source template; `dotnet new` strips it from a
# generated repository. Require template.json only when the .template.config directory is present,
# so the same validator passes in both the source template and a generated solution.
$templateConfigDir = Join-Path $repoRoot '.template.config'
if ((Test-Path $templateConfigDir) -and -not (Test-Path (Join-Path $templateConfigDir 'template.json'))) {
    Add-Error "Missing required file: .template.config/template.json"
}

# 2) Test taxonomy: any file with a [Test] must declare a type and a suite (class- or method-level).
$testFiles = Get-ChildItem -Path (Join-Path $repoRoot 'tests/Application.Tests') -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '[\\/]Framework[\\/]' }
foreach ($file in $testFiles) {
    $text = Get-Content -Raw -Path $file.FullName
    if ($text -notmatch '\[Test\]|\[TestCase') { continue }
    if ($text -notmatch '\[TestType\(') { Add-Error "Test file lacks [TestType(...)]: $($file.Name)" }
    if ($text -notmatch '\[Suite\(') { Add-Error "Test file lacks [Suite(...)]: $($file.Name)" }
}

# 3) No committed secrets, absolute local paths, or leftover placeholder namespaces in source.
$scanRoots = @('src', 'tests', 'scripts', '.github') | ForEach-Object { Join-Path $repoRoot $_ }
$scanFiles = Get-ChildItem -Path $scanRoots -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in @('.cs', '.json', '.ps1', '.yml', '.yaml', '.props', '.sql') }

foreach ($file in $scanFiles) {
    $rel = $file.FullName.Substring($repoRoot.Length + 1)

    # This validator necessarily contains the very patterns it searches for.
    if ($file.Name -eq 'Validate-Template.ps1') { continue }

    $text = Get-Content -Raw -Path $file.FullName

    # Absolute Windows user paths must never be committed.
    if ($text -match '[A-Za-z]:\\Users\\') { Add-Error "Absolute local path committed in $rel" }

    # Leftover placeholder namespace from the design blueprint.
    if ($text -match 'Company\.Product') { Add-Error "Leftover placeholder namespace 'Company.Product' in $rel" }

    # Test fixtures legitimately contain fake credentials to exercise redaction; only committed
    # real secrets in source/config are a problem, so skip the credential heuristics under tests/.
    $isTest = $rel -match '^tests[\\/]'
    if (-not $isTest) {
        # Connection-string password with a real (non-placeholder) value.
        foreach ($m in [regex]::Matches($text, '(?i)(?:Password|Pwd)\s*=\s*([^;"''\s]+)')) {
            $value = $m.Groups[1].Value
            if ($value -and $value -notmatch 'REPLACE_WITH|secret;?$|^\$') {
                Add-Error "Possible committed SQL password in $rel"
            }
        }

        # A bearer token literal (not a GitHub secret reference or placeholder).
        if ([regex]::IsMatch($text, 'Bearer\s+[A-Za-z0-9\-._~+/]{20,}=*')) {
            Add-Error "Possible committed bearer token in $rel"
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host "Template validation FAILED:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host " - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "Template validation passed." -ForegroundColor Green
