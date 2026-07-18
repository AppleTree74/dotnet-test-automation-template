# Reusable .NET Test Automation Template

A reusable GitHub + `dotnet new` template for browser, REST API, read-only Microsoft SQL Server,
and end-to-end tests on **.NET 10**, with **Playwright**, **NUnit**, **Allure Report 3**, and
**GitHub Actions**. It is designed to be understood and safely maintained by human engineers,
OpenAI Codex, and Claude Code.

## Highlights

- Layered architecture with strict downward dependencies (see [`docs/architecture.md`](docs/architecture.md)).
- Fresh Playwright `BrowserContext` per UI test; failure evidence (screenshot, trace, URL, console,
  page HTML) captured automatically.
- Generic REST client on `IHttpClientFactory` with bearer auth in a delegating handler and
  sanitized diagnostics.
- Read-only, parameterized SQL only — no DML, schema changes, stored procedures, or raw connections.
- Central secret redaction before any log, attachment, or report.
- Stable command surface (`scripts/Invoke-Tests.ps1`) shared by humans, agents, and CI.
- Allure Report 3 published to GitHub Pages with durable single-file history.

## Prerequisites

- .NET SDK pinned in [`global.json`](global.json) (.NET 10).
- Node.js (see [`.nvmrc`](.nvmrc)) for Allure Report 3.
- PowerShell 7+ (`pwsh`) for the scripts.

## Quick start

```powershell
dotnet restore AutomationTemplate.slnx --locked-mode
dotnet build AutomationTemplate.slnx -c Release --no-restore

# Framework unit/structural tests (no browser, no secrets)
dotnet test tests/Automation.UnitTests/Automation.UnitTests.csproj -c Release --no-build

# Install a browser, then run the Chromium smoke suite
pwsh ./scripts/Install-Playwright.ps1 -Browser chromium
pwsh ./scripts/Invoke-Tests.ps1 -Type UI -Suite Smoke -Browser chromium -Workers 4

# Generate the Allure report
pwsh ./scripts/Generate-Allure.ps1
```

The bundled `FrameworkBrowserSmokeTests` runs with no external Test URL (it exercises the browser
stack against in-memory page content). Product sample tests are `[Ignore]`d until you configure a
Test URL, API, and SQL — see [`docs/configuration.md`](docs/configuration.md).

## Using this template

- **GitHub**: click **Use this template** to create a new repository. Then set the Test Environment
  variables/secrets from [`docs/configuration.md`](docs/configuration.md), record a
  `TEMPLATE_VERSION`, and follow the upgrade guide for future template changes.
- **dotnet new**:
  ```powershell
  dotnet new install .
  dotnet new test-automation -n MyProduct.Automation
  ```

## Documentation

| Doc | Purpose |
|---|---|
| [`AGENTS.md`](AGENTS.md) | Canonical operational guide (Codex and compatible agents). |
| [`CLAUDE.md`](CLAUDE.md) | Claude Code entry point. |
| [`docs/architecture.md`](docs/architecture.md) | Projects, dependency rules, composition. |
| [`docs/configuration.md`](docs/configuration.md) | Options, precedence, secrets. |
| [`docs/test-standards.md`](docs/test-standards.md) | Taxonomy, fixtures, isolation, safety. |
| [`docs/debugging.md`](docs/debugging.md) | Evidence-led debugging loop and artifact map. |
| [`docs/upgrade-guide.md`](docs/upgrade-guide.md) | Pulling template changes into a generated repo. |
| [`CHANGELOG.md`](CHANGELOG.md) | Template version history. |

## CI

- `validate.yml` — locked restore, Release build, format check, framework tests, template
  validation. No Test secrets; no browsers.
- `test-and-report.yml` — manual dispatch (runner, browser, type, suite, tags, parallelism);
  conditional browser install; always collects diagnostics; publishes Allure 3 to GitHub Pages with
  durable history; preserves the original test exit status. A disabled UTC schedule example is
  included.

## Safety

Read-only SQL, secret redaction, no assertions in framework/product mechanics, no self-healing
locators, and no automatic external LLM calls from CI. See [`AGENTS.md`](AGENTS.md) for the full
rules.
