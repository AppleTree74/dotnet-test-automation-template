# Upgrade guide

GitHub template repositories do not receive upstream changes automatically. A generated repository
should track the template version it started from and pull changes deliberately.

## Record your template version

The generated repository inherits [`TEMPLATE_VERSION`](../TEMPLATE_VERSION). Keep it accurate; it is
the anchor for future upgrades.

## Pulling template changes

1. Read the template [`CHANGELOG.md`](../CHANGELOG.md) entries newer than your `TEMPLATE_VERSION`.
2. Apply framework changes under `src/Automation.*` and shared config (`Directory.*.props`,
   `global.json`, workflows, scripts). These rarely conflict with product code.
3. Re-run locked restore and the Release build:
   ```powershell
   dotnet restore AutomationTemplate.slnx --locked-mode
   dotnet build AutomationTemplate.slnx -c Release --no-restore
   ```
4. Run the framework unit tests and a Chromium smoke:
   ```powershell
   dotnet test tests/Automation.UnitTests/Automation.UnitTests.csproj -c Release --no-build
   pwsh ./scripts/Invoke-Tests.ps1 -Type UI -Suite Smoke -Browser chromium -Workers 4
   ```
5. Update `TEMPLATE_VERSION` to the version you upgraded to.

## What is safe to customise

- `src/Application.Automation` (Pages, components, typed clients, reviewed `.sql`, workflows).
- `tests/Application.Tests` (fixtures, intent, assertions, categories, Allure hierarchy).
- Configuration values (URLs, secret names) per [`docs/configuration.md`](configuration.md).

## What to change only via a decision record

When you must diverge from an approved framework decision, record the reason in `docs/decisions/`
rather than silently editing shared mechanics. See
[ADR 0001](decisions/0001-artifact-and-allure-layout.md) for the format.

## Package and action versions

Revalidate NuGet, npm, and GitHub Action versions before a template release. Do not copy stale
version numbers; update `Directory.Packages.props`, `package.json`, and pinned action refs together
and refresh the lock files.
