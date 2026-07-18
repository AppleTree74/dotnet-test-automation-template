using System.Net;
using System.Text;
using Automation.Api;
using Automation.Core.Configuration;
using Automation.Core.Redaction;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Automation.UnitTests.Api;

[TestFixture]
public sealed class ApiClientTests
{
    private sealed record Sample(int Id, string Name);

    private static ApiClient CreateClient(StubHttpMessageHandler stub, int timeoutMs = 30_000)
    {
        var httpClient = new HttpClient(stub)
        {
            BaseAddress = new Uri("https://api.test.example.invalid"),
            Timeout = Timeout.InfiniteTimeSpan,
        };
        var redactor = new Redactor(new RedactionOptions());
        var options = new ApiOptions { BaseUrl = "https://api.test.example.invalid", TimeoutMs = timeoutMs };
        return new ApiClient(httpClient, redactor, options, NullLogger<ApiClient>.Instance);
    }

    [Test]
    public async Task SendAsync_DeserializesSuccessBody_AndSetsCorrelationId()
    {
        var stub = new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"id":7,"name":"widget"}""", Encoding.UTF8, "application/json"),
        }));
        ApiClient client = CreateClient(stub);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/items/7", UriKind.Relative));
        ApiResponse<Sample> response = await client.SendAsync<Sample>(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.IsSuccess, Is.True);
            Assert.That(response.Data, Is.EqualTo(new Sample(7, "widget")));
            Assert.That(response.Diagnostics.CorrelationId, Is.Not.Empty);
            Assert.That(stub.LastRequest!.Headers.Contains(ApiClient.CorrelationIdHeader), Is.True);
            Assert.That(response.Diagnostics.ElapsedMs, Is.GreaterThanOrEqualTo(0));
        });
    }

    [Test]
    public async Task SendAsync_DoesNotThrowOnFailure_AndSanitizesResponseBody()
    {
        var stub = new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":"bad","password":"hunter2"}""", Encoding.UTF8, "application/json"),
        }));
        ApiClient client = CreateClient(stub);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/items", UriKind.Relative));
        ApiResponse<Sample> response = await client.SendAsync<Sample>(request);

        Assert.Multiple(() =>
        {
            Assert.That((int)response.StatusCode, Is.EqualTo(400));
            Assert.That(response.IsSuccess, Is.False);
            Assert.That(response.Diagnostics.SanitizedResponseBody, Does.Not.Contain("hunter2"));
            Assert.That(response.RawBody, Does.Not.Contain("hunter2"));
        });
    }

    [Test]
    public async Task SendAsync_ReturnsRequestTimeout_WhenTimeoutElapses()
    {
        var stub = new StubHttpMessageHandler(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        ApiClient client = CreateClient(stub, timeoutMs: 100);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/slow", UriKind.Relative));
        ApiResponse<Sample> response = await client.SendAsync<Sample>(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.RequestTimeout));
            Assert.That(response.Diagnostics.Error, Is.Not.Null);
        });
    }

    [Test]
    public void SendAsync_HonorsCallerCancellation()
    {
        var stub = new StubHttpMessageHandler(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        ApiClient client = CreateClient(stub);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/slow", UriKind.Relative));

        Assert.ThrowsAsync<TaskCanceledException>(async () => await client.SendAsync<Sample>(request, cts.Token));
    }
}
