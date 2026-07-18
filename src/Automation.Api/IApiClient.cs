namespace Automation.Api;

/// <summary>
/// Generic REST client contract (guide section 9.1). Sends a fully-formed request and returns a
/// typed response with enough data for a test to assert. The client MUST NOT assert, MUST NOT
/// auto-retry ordinary HTTP failures, and MUST NOT throw on non-success status codes.
/// </summary>
public interface IApiClient
{
    Task<ApiResponse<T>> SendAsync<T>(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default);
}

/// <summary>Supplies bearer access tokens. Rotation/acquisition is product-specific.</summary>
public interface ITokenProvider
{
    ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
