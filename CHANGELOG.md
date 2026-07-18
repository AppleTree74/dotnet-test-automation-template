# Changelog

All notable changes to this template are documented here. GitHub template changes do not propagate
automatically; generated repositories should track their `TEMPLATE_VERSION` and follow
[`docs/upgrade-guide.md`](docs/upgrade-guide.md).

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-07-18

### Added

- Initial scaffold: six-project solution on .NET 10 (`Automation.Core`, `Automation.Browser`,
  `Automation.Api`, `Automation.SqlServer`, `Application.Automation`, `Application.Tests`) plus
  `Automation.UnitTests`.
- Central package management with committed NuGet lock files; warnings as errors; nullable enabled.
- `Automation.Core`: validated options, run/test identity, artifact paths with containment safety,
  secret redaction, NLog console + per-test JSONL logging, run manifest (schema v1).
- `Automation.Browser`: Playwright driver, fresh context/page per test, failure evidence, Page and
  Component base conventions.
- `Automation.Api`: `IApiClient`, `ITokenProvider`, `BearerTokenHandler`, `ApiResponse<T>`,
  sanitized diagnostics, `IHttpClientFactory` wiring.
- `Automation.SqlServer`: `IReadOnlySqlClient`, `SqlQuery`, read-only command validation,
  `ApplicationIntent=ReadOnly`, embedded `.sql` resources, safe evidence.
- Test contract: NUnit category attributes (Type/Suite/Feature), one-Type/at-least-one-Suite
  validation, base fixtures, per-test artifact isolation, Allure Epic/Feature/Story.
- Scripts: `Invoke-Tests.ps1` (allow-listed inputs), `Install-Playwright.ps1`, `Generate-Allure.ps1`,
  `Validate-Template.ps1`.
- Allure Report 3 (`allurerc.mjs`) with durable single-file history; `package-lock.json` committed.
- GitHub Actions: `validate.yml` and `test-and-report.yml` (manual dispatch, conditional browser
  install, Pages deploy, durable history branch, exit-status preservation, commented UTC schedule).
- AI surfaces: `AGENTS.md`, `CLAUDE.md`, shared `docs/`, and thin write-tests/debug-tests skills for
  `.agents` and `.claude`.
- `.template.config/template.json` for `dotnet new`.

### Known gaps

- Product Test URL, API, and SQL are placeholders; sample product tests are `[Ignore]`d until
  configured.
- The scheduled workflow trigger is intentionally disabled pending day/time/IANA-timezone approval.
- `npm audit` reports advisories in Allure's transitive dependencies; revalidate at release time.
