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
| `Artifacts` | `ArtifactOptions` | Artifact root and Allure results directory name. |
| `Redaction` | `RedactionOptions` | Secret field names and mask text. |

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
