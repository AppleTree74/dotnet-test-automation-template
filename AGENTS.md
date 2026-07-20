# Repository guidance

## Mission

This repository is a reusable .NET 10 test automation template supporting Playwright browser tests, REST API tests through `HttpClient`, parameterized read-only Microsoft SQL Server verification, NUnit, Allure Report 3, and GitHub Actions.

Read `docs/architecture.md`, `docs/configuration.md`, `docs/test-standards.md`, and `docs/debugging.md` before changing shared framework behavior.

## Instruction precedence

1. The current user request and explicit application decisions.
2. This `AGENTS.md` and `CLAUDE.md`.
3. Shared documents under `docs/`.
4. Existing code conventions.

Do not invent product behavior, URLs, credentials, database details, schedules, or expected test outcomes. Ask when a missing answer would change architecture, security, or test meaning.

## Architecture boundaries

- Keep configuration, logging, identity, artifact paths, redaction, and shared contracts in `Automation.Core`.
- Keep Playwright startup, contexts, browser options, traces, screenshots, and console collection in `Automation.Browser`.
- Keep generic `HttpClient` setup, auth handlers, serialization, timing, and sanitized diagnostics in `Automation.Api`.
- Keep parameterized read-only query execution and mapping in `Automation.SqlServer`.
- Keep product pages, components, endpoints, DTOs, SQL resources, and business workflows in `Application.Automation`.
- Keep NUnit fixtures, test intent, assertions, categories, and Allure hierarchy metadata in `Application.Tests`.
- Dependencies flow downward. `Automation.Core` never references higher layers.
- Do not put NUnit assertions in Page Objects, API clients, SQL adapters, or shared workflows.

## Safety rules

- SQL is query-only. Do not add DML, schema changes, stored-procedure execution, sequence value generation (`NEXT VALUE FOR`), raw `SqlConnection` exposure, or interpolated SQL.
- Parameterize every SQL value and use a database identity with only required `SELECT` permissions.
- Do not commit or log credentials, tokens, cookies, connection strings, or secret payload fields.
- Redact before data is written to logs or Allure attachments.
- Validate workflow and script inputs against allowlists before constructing filters or commands.
- Do not weaken, skip, delete, or silently retry a failing test to obtain a pass.
- Do not implement runtime self-healing locators.

## Browser rules

- Every UI test receives a fresh Playwright `BrowserContext` and page.
- Prefer locators in this order: role, label, placeholder, text, test ID, stable CSS; XPath is last.
- Use Playwright auto-waiting and web-first expectations. Do not add fixed sleeps.
- Page Objects model pages; component objects model reusable areas.
- Public object methods express user behavior, not low-level click sequences.
- Capture failure evidence before disposing the browser context.

## API rules

- Use `IHttpClientFactory`, `HttpClient`, and `System.Text.Json`.
- Keep bearer authentication in a delegating handler using an `ITokenProvider`.
- Do not mutate shared default request headers per test.
- Use cancellation tokens and explicit timeouts.
- Do not automatically retry ordinary HTTP status failures.
- Store product endpoints and DTOs in `Application.Automation`.

## Test taxonomy

- Every test has exactly one type: `UI`, `API`, `Database`, or `E2E`.
- Every test has at least one suite: `Smoke` or `Regression`.
- Feature categories use `Feature:<name>`.
- Allure hierarchy uses stable Epic, Feature, and Story values.
- Tests must run alone, in any order, and in parallel.

## Stable commands

Use the repository scripts. Do not create a second execution path for agents.

```powershell
pwsh ./scripts/Invoke-Tests.ps1 -Type UI -Suite Smoke -Browser chromium -Workers 4
pwsh ./scripts/Invoke-Tests.ps1 -Type API -Suite Regression -Workers 4
pwsh ./scripts/Invoke-Tests.ps1 -Type Database -Suite Smoke -Workers 2
pwsh ./scripts/Invoke-Tests.ps1 -Type E2E -Suite Smoke -Browser chromium -Workers 2
pwsh ./scripts/Invoke-Tests.ps1 -TestName <fully-or-uniquely-qualified-test-name>
npm run allure:generate
```

Framework maintenance commands (use the pinned SDK in `global.json` and locked dependencies):

```powershell
dotnet restore AutomationTemplate.slnx --locked-mode
dotnet build AutomationTemplate.slnx -c Release --no-restore
dotnet format AutomationTemplate.slnx --verify-no-changes --no-restore
dotnet test tests/Automation.UnitTests/Automation.UnitTests.csproj -c Release --no-build
pwsh ./scripts/Test-ExitContract.ps1        # authoritative exit-status regression
pwsh ./scripts/Install-Playwright.ps1 -Browser chromium
pwsh ./scripts/Generate-Allure.ps1
pwsh ./scripts/Validate-Template.ps1
pwsh ./scripts/Test-TemplateGeneration.ps1  # generate a repo and validate it end to end
```

## Change workflow

1. Inspect the relevant implementation, tests, configuration, and recent diagnostics.
2. State any material assumption. Ask if it would change product meaning or safety.
3. Make the smallest coherent change within the correct module.
4. Add or update independent tests and documentation.
5. Run the smallest affected test, then the relevant suite.
6. For framework changes, run structural/unit checks plus Chromium smoke; run additional browsers when browser-shared behavior changes.
7. Confirm no secret or unsupported SQL surface was introduced.
8. Report what changed, what passed, and any remaining risk.

## Debugging workflow

1. Read `run-manifest.json` and Allure results.
2. Identify the first meaningful failure rather than secondary teardown noise.
3. Inspect relevant evidence: trace, screenshot, URL, console, JSONL logs, API evidence, or SQL evidence.
4. Reproduce the smallest failing test with exact inputs.
5. Diagnose root cause before editing.
6. Apply the smallest correction that preserves test intent.
7. Rerun the failing test and relevant suite.

Do not claim a fix without verification. If environment access, credentials, or product behavior blocks verification, report the blocker precisely.

## CI and reporting invariants

- Ubuntu 24.04 and Chromium are routine defaults; Windows 2025 is selectable.
- API and Database runs do not install browsers.
- Manual inputs are allowlisted: runner, browser, type, suite, tags, and parallelism.
- The schedule remains disabled until days, time, and IANA timezone are approved.
- Scheduled runs use Chromium only unless policy is explicitly changed.
- Reporting and artifact collection run even after test failure, but the original test exit status remains authoritative.
- Allure 3 HTML publishes to GitHub Pages and its single `history.jsonl` persists between runs.
- Raw diagnostics are workflow artifacts for interactive human/Codex/Claude diagnosis; CI does not send them to an external model.

## Completion checklist

- Relevant tests pass.
- The relevant suite passes.
- Documentation reflects command, configuration, category, dependency, or architecture changes.
- No stale Allure data is mixed into the current result set.
- No secrets or unsafe SQL appear in source, logs, or attachments.
- The change respects module boundaries and remains safe under parallel execution.

