# ADR 0001 — Artifact and Allure results layout

Status: accepted (2026-07-18)

## Context

[`AI_IMPLEMENTATION_GUIDE.md`](../AI_IMPLEMENTATION_GUIDE.md) section 7.2 shows `allure-results/` nested under
`artifacts/<run-id>/`, while the run-manifest example in section 15.3 references a top-level
`allure-results` path. Allure's tooling and the durable-history flow in section 14 assume a
single, stable results directory that is cleaned before each independent launch.

## Decision

- Allure's live results directory is the top-level `allure-results/` (Allure's default),
  cleaned before each run. This matches the manifest example and the section 14 history flow.
- `artifacts/<run-id>/` owns `run-manifest.json`, `test-results.trx`, and
  `tests/<test-id>/...` raw evidence.
- Allure attachments reference the raw evidence files under `artifacts/<run-id>/tests/...`.

The two design references are reconciled in favour of the manifest example because Allure
history and report generation depend on a stable, cleanable results path.

## Consequences

`ArtifactPaths.AllureResultsDirectory` resolves relative to the repository root, not the run
root. Everything else stays under `artifacts/<run-id>/`.
