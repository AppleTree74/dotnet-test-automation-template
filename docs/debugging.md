# Debugging

Diagnose from evidence, not guesses. CI never sends artifacts to an external model; AI diagnosis is
interactive and uses the workflow artifacts described here.

## Evidence-led loop

1. Read `artifacts/<run-id>/run-manifest.json` (schema v1) and the Allure result.
2. Identify the first meaningful failure, not secondary teardown noise.
3. Inspect the relevant evidence for that test under
   `artifacts/<run-id>/tests/<test-id>/`:
   - `test-log.jsonl` — structured per-test log
   - `screenshot.png`, `trace.zip`, `page.html`, `current-url.txt` — UI failures
   - `browser-console.jsonl` — console errors (always) / full console (on failure)
   - `api-evidence.json` — sanitized method, URL, status, timing, correlation id, bounded body
   - `sql-evidence.json` — query id, elapsed, row count, parameter names (never values)
4. Reproduce the smallest failing test with exact inputs:
   ```powershell
   pwsh ./scripts/Invoke-Tests.ps1 -TestName <unique-test-name>
   ```
5. Diagnose the root cause before editing.
6. Apply the smallest correction that preserves test intent.
7. Rerun the failing test, then the relevant suite.

Do not claim a fix without verification. If environment access, credentials, or product behaviour
blocks verification, report the blocker precisely. Do not weaken, skip, retry, or delete a test
merely to obtain green unless the owner explicitly approves a change in expected behaviour.

## Artifact map

```text
artifacts/<run-id>/
  run-manifest.json        # runId, commit, runner, type/suite/browser, result, paths
  test-results.trx         # NUnit results
  tests/<test-id>/         # per-test evidence (see above)
allure-results/            # Allure 3 results (top-level, cleaned per run)
allure-report/             # generated HTML report (npm run allure:generate)
allure-history/history.jsonl  # durable single-file history
```

## Playwright traces

Open a captured trace locally:

```powershell
pwsh tests/Application.Tests/bin/Release/net10.0/playwright.ps1 show-trace artifacts/<run-id>/tests/<test-id>/trace.zip
```

## Allure Agent Mode

Allure Report 3 provides an interactive agent mode for AI-assisted diagnosis:

```powershell
npx allure agent --help
```

It is interactive and is not part of routine report generation.

## Reproduce a deliberate failure

`EvidenceDemoTests` is an `[Explicit]` deliberately failing UI test that produces the full evidence
set. Run it to inspect what evidence looks like:

```powershell
pwsh ./scripts/Invoke-Tests.ps1 -TestName Evidence_OnFailure_IsCaptured
```
