---
name: write-tests
description: Add or modify a UI, API, Database, or E2E test in this .NET automation template following the shared architecture and test standards.
---

# Write a test

This skill wraps one canonical workflow shared with Claude Code and human engineers. It does not
restate architecture rules; the shared documents are the source of truth.

Read first: [`AGENTS.md`](../../../AGENTS.md), [`docs/test-standards.md`](../../../docs/test-standards.md),
[`docs/architecture.md`](../../../docs/architecture.md).

## Steps

1. Choose the base fixture by type: `UiTestBase` (UI/E2E), `ApiTestBase` (API), `DatabaseTestBase`
   (Database, read-only).
2. Tag with exactly one `[TestType(...)]`, at least one `[Suite(...)]`, optional `[Feature("...")]`,
   plus `[AllureNUnit]` and stable `[AllureEpic]`/`[AllureFeature]`/`[AllureStory]`.
3. Keep product behaviour in `Application.Automation`; assertions live only in the test.
4. Do not invent URLs, credentials, database details, or expected outcomes. Keep unknowns as
   `[Ignore("reason")]` and ask.
5. Use Playwright auto-waiting and web-first assertions; no fixed sleeps; no self-healing locators.
6. Run:
   ```powershell
   pwsh ./scripts/Invoke-Tests.ps1 -TestName <unique-name>
   pwsh ./scripts/Invoke-Tests.ps1 -Type <UI|API|Database|E2E> -Suite Smoke
   ```

If a command or rule is missing, update the shared docs and `AGENTS.md` — do not create a
tool-only convention.
