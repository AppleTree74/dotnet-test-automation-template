using System.Net;
using Automation.Api;
using NUnit.Framework;

namespace Automation.UnitTests.Api;

[TestFixture]
public sealed class BearerTokenHandlerTests
{
    private static HttpClient CreateClient(string? token, out StubHttpMessageHandler stub)
    {
        stub = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var handler = new BearerTokenHandler(new StaticTokenProvider(token)) { InnerHandler = stub };
        return new HttpClient(handler) { BaseAddress = new Uri("https://api.test.example.invalid") };
    }

    [Test]
    public async Task AttachesBearerHeader_WhenTokenPresent()
    {
        HttpClient client = CreateClient("secret-token", out StubHttpMessageHandler stub);

        await client.GetAsync(new Uri("/ping", UriKind.Relative));

        Assert.That(stub.LastRequest!.Headers.Authorization, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(stub.LastRequest.Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
            Assert.That(stub.LastRequest.Headers.Authorization.Parameter, Is.EqualTo("secret-token"));
        });
    }

    [Test]
    public async Task OmitsBearerHeader_WhenTokenBlank()
    {
        HttpClient client = CreateClient(token: "", out StubHttpMessageHandler stub);

        await client.GetAsync(new Uri("/ping", UriKind.Relative));

        Assert.That(stub.LastRequest!.Headers.Authorization, Is.Null);
    }
}
