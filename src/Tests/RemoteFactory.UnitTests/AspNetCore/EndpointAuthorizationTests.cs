using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using Neatoo.RemoteFactory.Internal;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.AspNetCore;

/// <summary>
/// Desk authorization, decided by the desk id: desk 2 is refused by a bool check, which
/// gives no reason; desk 3 is refused with one.
/// </summary>
public interface IDeskAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    bool IsOpen(int id);

    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    string? Reason(int id);
}

public sealed class DeskAuth : IDeskAuth
{
    public const string ClosedReason = "Desk 3 is closed for cleaning.";

    public bool IsOpen(int id) => id != 2;

    public string? Reason(int id) => id == 3 ? ClosedReason : null;
}

[Factory]
[AuthorizeFactory<IDeskAuth>]
public interface IDesk
{
    Task<string> Open(int id);

    Task Close(int id);
}

public sealed class Desk : IDesk
{
    public Task<string> Open(int id) => Task.FromResult($"desk {id} open");

    public Task Close(int id) => Task.CompletedTask;
}

[Factory]
[AuthorizeFactory<IDeskAuth>]
public static partial class DeskCommands
{
    [Remote, Execute]
    private static Task<string> _Ring(int id) => Task.FromResult($"desk {id} rang");
}

/// <summary>
/// An [AuthorizeFactory] refusal thrown on the server, over UseNeatoo's endpoint (#103).
/// An interface-factory method and a static [Execute] have no Authorized&lt;T&gt; to carry a
/// refusal, so their auth checks throw NotAuthorizedException on the server. The client must
/// receive that exception, not an HttpRequestException for a 500 it cannot tell from a crash.
/// </summary>
/// <remarks>
/// The host runs in Production, as EndpointCancellationTests' does, and its outermost
/// middleware records what request logging would see. It also ends an escaped exception
/// as Kestrel does, so the client sees what it would over a real hop.
/// </remarks>
public sealed class EndpointAuthorizationTests
{
    private sealed record RequestOutcome(int StatusCode, Exception? Escaped);

    private sealed class Host : IAsyncDisposable
    {
        private readonly WebApplication app;
        private readonly TaskCompletionSource<RequestOutcome> outcome = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private Host(bool forbidEveryRequest)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
            builder.WebHost.UseTestServer();
            builder.Logging.AddProvider(ServerLogs);
            builder.Services.AddNeatooAspNetCore(typeof(IDesk).Assembly);
            builder.Services.AddScoped<IDeskAuth, DeskAuth>();
            builder.Services.AddScoped<IDesk, Desk>();
            app = builder.Build();

            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                    outcome.TrySetResult(new RequestOutcome(context.Response.StatusCode, null));
                }
                catch (Exception ex)
                {
                    // Ended as Kestrel ends it: a 500 with no body. Rethrown, TestServer would
                    // hand the server's exception object to the client, which no real hop can.
                    outcome.TrySetResult(new RequestOutcome(StatusCodes.Status500InternalServerError, ex));
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            });
            if (forbidEveryRequest)
            {
                // A 403 that is not RemoteFactory's: an authentication scheme's forbid, say.
                app.Use((HttpContext context, RequestDelegate _) =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                });
            }
            app.UseNeatoo();
        }

        public CapturingLoggerProvider ServerLogs { get; } = new();

        public CapturingLoggerProvider ClientLogs { get; } = new();

        /// <summary>Completes when the server has finished the request.</summary>
        public Task<RequestOutcome> Outcome => outcome.Task;

        public static async Task<Host> Start(bool forbidEveryRequest = false)
        {
            var host = new Host(forbidEveryRequest);
            await host.app.StartAsync(TestContext.Current.CancellationToken);
            return host;
        }

        public IServiceProvider CreateClient()
        {
            var services = new ServiceCollection();
            services.AddLogging(logging => logging.AddProvider(ClientLogs));
            services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IDesk).Assembly);
            services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) => app.GetTestServer().CreateClient());
            return services.BuildServiceProvider().CreateScope().ServiceProvider;
        }

        public async ValueTask DisposeAsync()
        {
            // UseNeatoo put this host's logger factory in a process-wide static (#155).
            NeatooLogging.SetLoggerFactory(null);
            await app.DisposeAsync();
        }
    }

    private static async Task<RequestOutcome> Completed(Host host)
        => await host.Outcome.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

    [Fact]
    public async Task InterfaceMethod_Allowed_ReturnsItsResult()
    {
        await using var host = await Host.Start();
        var desk = host.CreateClient().GetRequiredService<IDeskFactory>();

        Assert.Equal("desk 1 open", await desk.Open(1));
    }

    /// <summary>The issue's path: a bool check refuses, so the message is the framework's.</summary>
    [Fact]
    public async Task InterfaceMethod_RefusedByABoolCheck_ClientThrowsTheServersException()
    {
        await using var host = await Host.Start();
        var desk = host.CreateClient().GetRequiredService<IDeskFactory>();

        var ex = await Assert.ThrowsAsync<NotAuthorizedException>(() => desk.Open(2));

        Assert.Equal("operation Open on IDesk (IDeskAuth.IsOpen)", ex.Context);
        Assert.Equal("Authorization denied for operation Open on IDesk (IDeskAuth.IsOpen); no reason was supplied.", ex.Message);
        Assert.NotNull(ex.Authorized);
        Assert.False(ex.Authorized.HasAccess);
        Assert.Null(ex.Authorized.Message);

        var outcome = await Completed(host);
        Assert.Null(outcome.Escaped);
        Assert.Equal(StatusCodes.Status403Forbidden, outcome.StatusCode);
    }

    [Fact]
    public async Task InterfaceMethod_RefusedWithAReason_ClientGetsTheReasonVerbatim()
    {
        await using var host = await Host.Start();
        var desk = host.CreateClient().GetRequiredService<IDeskFactory>();

        var ex = await Assert.ThrowsAsync<NotAuthorizedException>(() => desk.Open(3));

        Assert.Equal(DeskAuth.ClosedReason, ex.Message);
        Assert.Equal("operation Open on IDesk (IDeskAuth.Reason)", ex.Context);
        Assert.Equal(DeskAuth.ClosedReason, ex.Authorized?.Message);
    }

    /// <summary>A method returning Task reads no result, and takes the same path.</summary>
    [Fact]
    public async Task InterfaceMethodReturningTask_Refused_ClientThrowsNotAuthorized()
    {
        await using var host = await Host.Start();
        var desk = host.CreateClient().GetRequiredService<IDeskFactory>();

        var ex = await Assert.ThrowsAsync<NotAuthorizedException>(() => desk.Close(2));

        Assert.Equal("operation Close on IDesk (IDeskAuth.IsOpen)", ex.Context);
    }

    [Fact]
    public async Task StaticExecute_Refused_ClientThrowsNotAuthorized()
    {
        await using var host = await Host.Start();
        var ring = host.CreateClient().GetRequiredService<DeskCommands.Ring>();

        Assert.Equal("desk 1 rang", await ring(1));
        var ex = await Assert.ThrowsAsync<NotAuthorizedException>(() => ring(3));

        Assert.Equal(DeskAuth.ClosedReason, ex.Message);
    }

    /// <summary>A refusal is not a failure, on either tier.</summary>
    [Fact]
    public async Task Refusal_IsLoggedAsARefusal_NotAsAnError()
    {
        await using var host = await Host.Start();
        var desk = host.CreateClient().GetRequiredService<IDeskFactory>();

        await Assert.ThrowsAsync<NotAuthorizedException>(() => desk.Open(3));
        await Completed(host);

        var server = Assert.Single(host.ServerLogs.Entries, e => e.EventId == 7009);
        Assert.Equal(LogLevel.Warning, server.Level);
        Assert.DoesNotContain(host.ServerLogs.Entries, e => e.EventId == 7005);
        var client = Assert.Single(host.ClientLogs.Entries, e => e.EventId == 3013);
        Assert.Equal(LogLevel.Information, client.Level);
        Assert.DoesNotContain(host.ClientLogs.Entries, e => e.Level >= LogLevel.Error);
    }

    /// <summary>
    /// A 403 RemoteFactory did not send carries no denial, and fails as any other status does.
    /// </summary>
    [Fact]
    public async Task ForbiddenByAnotherComponent_StillRaisesHttpRequestException()
    {
        await using var host = await Host.Start(forbidEveryRequest: true);
        var desk = host.CreateClient().GetRequiredService<IDeskFactory>();

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => desk.Open(1));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }
}
