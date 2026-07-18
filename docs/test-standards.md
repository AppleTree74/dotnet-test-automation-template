# Test standards

## Classification

Every test declares exactly one **type** and at least one **suite**, enforced at runtime by
`CategoryConventions.ResolveAndValidate`.

- Type (exactly one): `UI`, `API`, `Database`, `E2E` — via `[TestType(TestType.UI)]`.
- Suite (one or more): `Smoke`, `Regression` — via `[Suite(Suite.Smoke)]`.
- Feature (optional, repeatable): `[Feature("Login")]` → category `Feature:Login`.

Attributes may be placed on the fixture (inherited by its tests) or on a method.

## Allure hierarchy

Use stable `[AllureEpic]`, `[AllureFeature]`, and `[AllureStory]` values independent of execution
categories. Add `[AllureNUnit]` to each concrete fixture.

## Base fixtures

| Base | Use for | Provides |
|---|---|---|
| `UiTestBase` | UI, E2E | Fresh `BrowserSession`/`Page` per test; full failure evidence |
| `ApiTestBase` | API | Shared `IApiClient` via `SendAsync<T>`; `api-evidence.json` on failure |
| `DatabaseTestBase` | Database | `IReadOnlySqlClient` via `QueryAsync<T>`; safe `sql-evidence.json` |

All bases derive from `AutomationTestBase`, which validates categories, allocates a unique per-test
artifact directory, opens a per-test JSONL log scope, and records the outcome.

## Isolation and parallelism

- Tests run in parallel across fixtures (`[assembly: Parallelizable(ParallelScope.Fixtures)]`).
- Each test owns unique data and a unique artifact directory; no test depends on another's browser
  session, side effects, database writes, or artifact directory.
- Worker count is set at run time (`NUnit.NumberOfTestWorkers`) by `scripts/Invoke-Tests.ps1`.

## Object model and locators

- Locator preference: role, label, placeholder, text, test id, stable CSS; XPath last.
- Page Objects model whole pages; components model reusable regions.
- Public methods express user behaviour, not low-level click sequences.
- No assertions in Page Objects, components, clients, adapters, or workflows.
- Use Playwright auto-waiting and web-first assertions; never add fixed sleeps or runtime
  self-healing locators.

## Safety

- SQL is query-only and parameterized. No DML, schema changes, stored procedures, interpolated
  SQL, or raw `SqlConnection`.
- Never commit or log secrets. Redaction happens before any log or attachment is written.
- Do not weaken, skip, delete, or silently retry a failing test to obtain a pass.

## Running tests

```powershell
pwsh ./scripts/Invoke-Tests.ps1 -Type UI -Suite Smoke -Browser chromium -Workers 4
pwsh ./scripts/Invoke-Tests.ps1 -Type API -Suite Regression -Workers 4
pwsh ./scripts/Invoke-Tests.ps1 -Type Database -Suite Smoke -Workers 2
pwsh ./scripts/Invoke-Tests.ps1 -Type E2E -Suite Smoke -Browser chromium -Workers 2
pwsh ./scripts/Invoke-Tests.ps1 -TestName Page_RendersContent_AndLocatorsResolve
npm run allure:generate
```
