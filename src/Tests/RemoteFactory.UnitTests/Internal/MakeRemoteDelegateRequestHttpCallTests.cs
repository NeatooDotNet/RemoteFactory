using System.Net;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// The client's HTTP call to /api/neatoo, against a stub server.
/// </summary>
/// <remarks>
/// A non-success answer throws HttpRequestException. Its StatusCode was null until
/// EXSET-006: the exception was built from the message alone, so a caller could read
/// the status only by parsing the message text.
/// </remarks>
public class MakeRemoteDelegateRequestHttpCallTests
{
    private sealed class StubServer(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    private static MakeRemoteDelegateRequestHttpCall CallTo(StubServer server, out ICorrelationContext correlation)
    {
        correlation = new CorrelationContextImpl { CorrelationId = "corr-1" };
        var http = new HttpClient(server) { BaseAddress = new Uri("http://server.test/") };
        return MakeRemoteDelegateRequestHttpCallImplementation.Create(http, correlation);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task NonSuccessStatus_ThrowsHttpRequestException_CarryingTheStatusCode(HttpStatusCode status)
    {
        using var server = new StubServer(status, "server said no");
        var call = CallTo(server, out _);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => call(new RemoteRequestDto { DelegateFullName = "Some.Delegate" }, TestContext.Current.CancellationToken));

        Assert.Equal(status, ex.StatusCode);
        Assert.Contains("server said no", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Call_PostsToTheEndpoint_WithTheCorrelationIdHeader()
    {
        using var server = new StubServer(HttpStatusCode.InternalServerError, "");
        var call = CallTo(server, out _);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => call(new RemoteRequestDto { DelegateFullName = "Some.Delegate" }, TestContext.Current.CancellationToken));

        Assert.Equal(HttpMethod.Post, server.LastRequest!.Method);
        Assert.Equal("http://server.test/api/neatoo", server.LastRequest.RequestUri!.ToString());
        Assert.Equal("corr-1", Assert.Single(server.LastRequest.Headers.GetValues("X-Correlation-Id")));
    }
}
