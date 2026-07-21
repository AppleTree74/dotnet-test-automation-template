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

Raw **binary** evidence cannot be centrally redacted, so whether each file is attached to the report
— and therefore published to GitHub Pages — is policy-gated by `Artifacts` options:

| Option | Default | File | Rationale |
|---|---:|---|---|
| `AttachScreenshotToReport` | `true` | `screenshot.png` | The most useful at-a-glance diagnostic; exposes only what was on screen. |
| `AttachTraceToReport` | `false` | `trace.zip` | Contains full DOM snapshots, page sources, and network bodies that cannot be sanitized. |
| `AttachHarToReport` | `false` | `network.har` | Raw request/response archive. |
| `AttachVideoToReport` | `false` | `video.webm` | Full-session recording. |

The default assumes **GitHub Pages is access-controlled to an internal audience**. Screenshots are
published for that audience; the trace, HAR, and video stay out of the report by default and are
retrieved from workflow artifacts for deep debugging. If Pages could ever be public — or the
application renders sensitive data on screen — set `AttachScreenshotToReport` to `false` for a
fully text-only report. To include the trace in the report for a trusted internal audience, set
`AttachTraceToReport` to `true`. These flags control **attachment only**; capture is unchanged, so
raw diagnostics remain in workflow artifacts either way.

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
