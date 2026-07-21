# Claude Code repository entry point

Read and follow `AGENTS.md` first. It is the canonical operational guide for this repository. Then read the relevant shared documents under `docs/`. The requirements/design baseline is `docs/AI_IMPLEMENTATION_GUIDE.md`, whose numbered sections are cited throughout the source ("guide section X").

## Working contract

- Use the same scripts and commands as humans, Codex, and GitHub Actions.
- Do not infer product behavior, Test URLs, credentials, database details, schedules, or expected outcomes.
- Keep framework mechanics separate from product automation and NUnit assertions.
- Preserve fresh Playwright context isolation, read-only parameterized SQL, secret redaction, parallel safety, and the authoritative test exit status.
- Diagnose from the run manifest, Allure results, JSONL logs, traces, screenshots, and sanitized API/SQL evidence before editing.
- Make the smallest scoped correction and verify the failing test plus its relevant suite before declaring success.

## Common commands

```powershell
pwsh ./scripts/Invoke-Tests.ps1 -Type UI -Suite Smoke -Browser chromium -Workers 4
pwsh ./scripts/Invoke-Tests.ps1 -Type API -Suite Regression -Workers 4
pwsh ./scripts/Invoke-Tests.ps1 -Type Database -Suite Smoke -Workers 2
pwsh ./scripts/Invoke-Tests.ps1 -TestName <test-name>
npm run allure:generate
```

If a command or architecture rule is missing, update the shared documentation and `AGENTS.md`; do not create a Claude-only convention.

