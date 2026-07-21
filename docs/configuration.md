# Configuration and secrets

The framework runs against one environment: `Test`. There is no environment selector; URLs and
secrets vary per generated application.

## Precedence

Lowest to highest:

1. committed non-secret `tests/Application.Tests/appsettings.json`
2. local `appsettings.local.json` (uncommitted per-developer overrides; Git-ignored — non-secret
   local settings only, never secrets)
3. local .NET user-secrets (developer machines; the place for local secrets)
4. environment variables prefixed `AUTOMATION__` (GitHub Environment values/secrets map here)
5. validated command inputs

All strongly typed options are validated before the first test (`OptionsValidation`). Placeholder
`*.invalid` URLs are allowed; sample integration tests skip with an explicit reason until real
values are supplied.

## Options

| Section | Type | Notes |
|---|---|---|
| `TestEnvironment` | `TestEnvironmentOptions` | Fixed name `Test`. |
| `Browser` | `BrowserOptions` | Base URL, default browser, headless, timeouts, video/HAR opt-in, `CapturePageHtml` (redacted page-HTML capture on failure; default on). |
| `Api` | `ApiOptions` | Base URL, timeout, bearer token (secret). |
| `SqlServer` | `SqlServerOptions` | Connection string (secret), command timeout, ReadOnly intent. |
| `Artifacts` | `ArtifactOptions` | Artifact root, Allure results directory name, and the report-attachment policy (below). |
| `Redaction` | `RedactionOptions` | Secret field names and mask text. |

## Evidence published to the report

Failure evidence is captured under `artifacts/<run-id>/tests/<test-id>/` and is always available in
full through the restricted CI workflow artifacts. Sanitized **text** evidence (current URL, console
JSONL, bounded page HTML, API and SQL evidence, logs) passes through the central redactor and is
always attached to the Allure report.

Raw **binary** evidence cannot be centrally redacted, so whether each kind is attached to the report
— and therefore published to GitHub Pages — is policy-gated by `Artifacts` options. All default to
**off**: no un-redactable binary is published unless you opt in. The decision is by **artifact type**
(file extension), so a Playwright-named video file (`page@<id>.webm`) is gated just like the canonical
`video.webm`.

| Option | Default | Type | Rationale |
|---|---:|---|---|
| `AttachScreenshotToReport` | `false` | `.png` | Pixels can show tokens, credentials, or personal data and cannot be redacted. |
| `AttachTraceToReport` | `false` | `.zip` | Full DOM snapshots, page sources, and network bodies that cannot be sanitized. |
| `AttachHarToReport` | `false` | `.har` | Raw request/response archive. |
| `AttachVideoToReport` | `false` | `.webm` | Full-session recording. |

These flags control **attachment only**; capture is unchanged, so raw diagnostics always remain in
the restricted workflow artifacts for interactive debugging. Enable one only after confirming Pages
access control and the application's data classification — e.g. set `AttachScreenshotToReport=true`
in a generated repository whose Pages is access-controlled and whose pages never render secrets.

### Report result sanitization

Attachment filtering does not cover the Allure **result JSON** itself. A Playwright/NUnit assertion
failure records `statusDetails` (message and trace) that can quote DOM, ARIA snapshots, locators, and
on-screen values; parameters, labels, and step names are free text too. Before the report is
generated, `tools/AllureResultsSanitizer` redacts a **copy** of `allure-results/` (via the shared
redactor) into `allure-results-sanitized/`, and the report and its durable history are generated from
that copy. The raw results stay untouched for workflow diagnostics, and sanitization **fails closed** —
a malformed result aborts publication rather than leaking. Do not place secrets in assertion messages,
test names, Allure titles, parameters, labels, or step names; sanitization is defense in depth, not a
license to embed them.

## Where settings live

| Setting | Location | Secret |
|---|---|---:|
| Web base URL | Test Environment variable `WEB_BASE_URL` → `AUTOMATION__Browser__BaseUrl` | No |
| API base URL | Test Environment variable `API_BASE_URL` → `AUTOMATION__Api__BaseUrl` | No |
| API bearer token | Test Environment secret `API_BEARER_TOKEN` → `AUTOMATION__Api__BearerToken` | Yes |
| SQL connection string | Test Environment secret `SQL_CONNECTION_STRING` → `AUTOMATION__SqlServer__ConnectionString` | Yes |
| Environment name | Fixed value `Test` | No |

## Local secrets

Use .NET user-secrets (never commit secrets):

```powershell
cd tests/Application.Tests
dotnet user-secrets init
dotnet user-secrets set "Api:BearerToken" "<token>"
dotnet user-secrets set "SqlServer:ConnectionString" "<read-only connection string>"
```

An override via environment variable uses the double-underscore form, e.g.
`AUTOMATION__Browser__BaseUrl=https://test.myapp.example`.

## SQL identity

The SQL connection must use a database identity granted only the required `SELECT` permissions.
`ApplicationIntent=ReadOnly` is applied where compatible, but database permissions — not
connection intent — are the authoritative control.
