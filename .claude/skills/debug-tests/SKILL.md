---
name: debug-tests
description: Diagnose a failing test in this .NET automation template using the evidence-led loop (run manifest, Allure results, traces, logs, sanitized API/SQL evidence). Use when a test fails or needs triage.
---

# Debug a failing test

This skill wraps the canonical evidence-led loop. The source of truth is
[`docs/debugging.md`](../../../docs/debugging.md); also see [`AGENTS.md`](../../../AGENTS.md).

## Loop

1. Read `artifacts/<run-id>/run-manifest.json` and the Allure result.
2. Identify the first meaningful failure, not teardown noise.
3. Inspect that test's evidence under `artifacts/<run-id>/tests/<test-id>/`:
   `test-log.jsonl`, `screenshot.png`, `trace.zip`, `page.html`, `current-url.txt`,
   `browser-console.jsonl`, `api-evidence.json`, `sql-evidence.json`.
4. Reproduce the smallest failing test:
   ```powershell
   pwsh ./scripts/Invoke-Tests.ps1 -TestName <unique-name>
   ```
5. Diagnose root cause before editing.
6. Apply the smallest correction that preserves test intent.
7. Rerun the failing test, then the relevant suite.

## Rules

- Never claim a fix without verification. If blocked (access, credentials, product behaviour),
  report the blocker precisely.
- Never weaken, skip, retry, or delete a test to obtain green unless the owner approves a change in
  expected behaviour.
- Open a trace with:
  `pwsh tests/Application.Tests/bin/Release/net10.0/playwright.ps1 show-trace <trace.zip>`.
