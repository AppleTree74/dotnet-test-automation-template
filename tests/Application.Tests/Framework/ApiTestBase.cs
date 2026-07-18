using Automation.Api;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Application.Tests.Framework;

/// <summary>
/// Base for API tests. Exposes the shared <see cref="IApiClient"/> and records sanitized request
/// diagnostics, writing <c>api-evidence.json</c> on failure. Uses no browser.
/// </summary>
public abstract class ApiTestBase : AutomationTestBase
{
    private readonly List<ApiRequestDiagnostics> _diagnostics = [];

    protected IApiClient Api => Services.GetRequiredService<IApiClient>();

    /// <summary>Sends a request and records its diagnostics for evidence.</summary>
    protected async Task<ApiResponse<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        ApiResponse<T> response = await Api.SendAsync<T>(request, cancellationToken);
        _diagnostics.Add(response.Diagnostics);
        return response;
    }

    [TearDown]
    public async Task ApiTearDown()
    {
        if (TestFailed && _diagnostics.Count > 0)
        {
            await ApiEvidenceWriter.WriteAsync(TestArtifactDirectory, _diagnostics);
        }
    }
}
