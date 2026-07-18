using System.Net.Http.Headers;

namespace Automation.Api;

/// <summary>
/// Delegating handler that attaches bearer authentication per request. Authentication lives here,
/// never in mutable shared default headers (guide section 9.2). A blank token (unconfigured
/// template) simply results in no Authorization header.
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly ITokenProvider _tokenProvider;

    public BearerTokenHandler(ITokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Default <see cref="ITokenProvider"/> that returns the statically configured token.</summary>
public sealed class StaticTokenProvider : ITokenProvider
{
    private readonly string _token;

    public StaticTokenProvider(string? token) => _token = token ?? string.Empty;

    public ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_token);
}
