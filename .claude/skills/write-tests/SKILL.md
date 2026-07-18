---
name: write-tests
description: Add or modify a UI, API, Database, or E2E test in this .NET automation template following the shared architecture and test standards. Use when asked to write, add, or update a test.
---

# Write a test

This skill wraps one canonical workflow. It does not restate architecture rules; follow the shared
documents, which are the source of truth.

Read first: [`AGENTS.md`](../../../AGENTS.md), [`docs/test-standards.md`](../../../docs/test-standards.md),
[`docs/architecture.md`](../../../docs/architecture.md).

## Steps

1. Choose the base fixture by test type:
   - UI or E2E → `UiTestBase` (fresh `Page` per test)
   - API → `ApiTestBase` (use `SendAsync<T>`)
   - Database → `DatabaseTestBase` (use `QueryAsync<T>`, read-only)
2. Tag the fixture or method:
   - exactly one `[TestType(TestType.X)]`
   - at least one `[Suite(Suite.Smoke)]` / `[Suite(Suite.Regression)]`
   - optional `[Feature("Name")]`
   - `[AllureNUnit]` plus `[AllureEpic]`/`[AllureFeature]`/`[AllureStory]`
3. Keep product behaviour in `Application.Automation` (Pages, components, typed clients, reviewed
   `.sql`, workflows). Assertions live only in the test.
4. Do not invent URLs, credentials, database details, or expected outcomes. If a value is unknown,
   keep the test skipped with an explicit `[Ignore("reason")]` and ask.
5. Use Playwright auto-waiting and web-first assertions; no fixed sleeps; no self-healing locators.
6. Run the smallest test, then the suite:
   ```powershell
   pwsh ./scripts/Invoke-Tests.ps1 -TestName <unique-name>
   pwsh ./scripts/Invoke-Tests.ps1 -Type <UI|API|Database|E2E> -Suite Smoke
   ```

If a command or rule is missing, update the shared docs and `AGENTS.md` — do not create a
tool-only convention.
