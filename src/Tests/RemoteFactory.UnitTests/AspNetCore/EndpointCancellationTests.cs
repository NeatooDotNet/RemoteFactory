using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.AspNetCore;

/// <summary>How a held call's server operation ends.</summary>
public enum HeldCallEnd
{
    /// <summary>Waits for its token, then returns normally: the work finished before the client left.</summary>
    ReturnWhenCancelled,

    /// <summary>Waits on its token and throws when it is cancelled.</summary>
    ThrowWhenCancelled,

    /// <summary>Throws OperationCanceledException at once, with no token cancelled.</summary>
    ThrowOwnCancellation,
}

/// <summary>The server's side of a held call: when the operation began, and how it ends.</summary>
public sealed class HeldCallControl(HeldCallEnd end)
{
    private readonly TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public HeldCallEnd End { get; } = end;

    /// <summary>Completes when the server operation begins.</summary>
    public Task Entered => entered.Task;

    internal void Enter() => entered.TrySetResult();
}

[Factory]
public static partial class HeldCalls
{
    [Remote, Execute]
    private static async Task<int> _Run(int value, [Service] HeldCallControl control, CancellationToken cancellationToken)
    {
        control.Enter();
        switch (control.End)
        {
            case HeldCallEnd.ThrowOwnCancellation:
                throw new OperationCanceledException();
            case HeldCallEnd.ThrowWhenCancelled:
                await Task.Delay(Timeout.Infinite, cancellationToken);
                break;
            default:
                var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using (cancellationToken.Register(() => cancelled.TrySetResult()))
                {
                    await cancelled.Task;
                }
                break;
        }
        return value;
    }
}

/// <summary>
/// UseNeatoo's endpoint when the call is cancelled (#116). A client that leaves, or a server
/// that is stopping, is not a server failure: the request must not end in an exception that
/// request logging (Serilog's UseSerilogRequestLogging, for one) records as a 500 at Error.
/// </summary>
/// <remarks>
/// The host runs in Production. In Development, ASP.NET Core's developer exception page
/// catches an aborted request's exception and answers 499 itself, which hides the bug.
/// The outermost middleware records what request logging would see.
/// </remarks>
public sealed class EndpointCancellationTests
{
    private sealed record RequestOutcome(int StatusCode, Exception? Escaped);

    private sealed class Host : IAsyncDisposable
    {
        private readonly WebApplication app;
        private readonly TaskCompletionSource<RequestOutcome> outcome = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private Host(HeldCallEnd end)
        {
            Control = new HeldCallControl(end);

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
            builder.WebHost.UseTestServer();
            builder.Services.AddNeatooAspNetCore(typeof(HeldCalls).Assembly);
            builder.Services.AddSingleton(Control);
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
                    outcome.TrySetResult(new RequestOutcome(context.Response.StatusCode, ex));
                    throw;
                }
            });
            app.UseNeatoo();
        }

        public HeldCallControl Control { get; }

        /// <summary>Completes when the server has finished the request.</summary>
        public Task<RequestOutcome> Outcome => outcome.Task;

        public IHostApplicationLifetime Lifetime => app.Lifetime;

        public static async Task<Host> Start(HeldCallEnd end)
        {
            var host = new Host(end);
            await host.app.StartAsync(TestContext.Current.CancellationToken);
            return host;
        }

        public HeldCalls.Run CreateClient()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(HeldCalls).Assembly);
            services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) => app.GetTestServer().CreateClient());
            return services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<HeldCalls.Run>();
        }

        public async ValueTask DisposeAsync()
        {
            // UseNeatoo put this host's logger factory in a process-wide static (#155).
            // Clear it before the factory is disposed, so tests running alongside do not
            // log through a disposed factory.
            NeatooLogging.SetLoggerFactory(null);
            await app.DisposeAsync();
        }
    }

    private static async Task<RequestOutcome> Completed(Host host)
        => await host.Outcome.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

    /// <summary>The issue's path: the operation finished, and the client left before the response.</summary>
    [Fact]
    public async Task ClientLeaves_AfterTheOperationFinished_RequestEndsQuietlyWith499()
    {
        await using var host = await Host.Start(HeldCallEnd.ReturnWhenCancelled);
        var run = host.CreateClient();

        using var cts = new CancellationTokenSource();
        var call = run(1, cts.Token);
        await host.Control.Entered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => call);
        var outcome = await Completed(host);
        Assert.Null(outcome.Escaped);
        Assert.Equal(499, outcome.StatusCode);
    }

    [Fact]
    public async Task ClientLeaves_DuringTheOperation_RequestEndsQuietlyWith499()
    {
        await using var host = await Host.Start(HeldCallEnd.ThrowWhenCancelled);
        var run = host.CreateClient();

        using var cts = new CancellationTokenSource();
        var call = run(1, cts.Token);
        await host.Control.Entered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => call);
        var outcome = await Completed(host);
        Assert.Null(outcome.Escaped);
        Assert.Equal(499, outcome.StatusCode);
    }

    /// <summary>The client is still there, so it receives the 503.</summary>
    [Fact]
    public async Task ServerStopping_DuringTheOperation_ClientReceives503()
    {
        await using var host = await Host.Start(HeldCallEnd.ThrowWhenCancelled);
        var run = host.CreateClient();

        var call = run(1, TestContext.Current.CancellationToken);
        await host.Control.Entered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        host.Lifetime.StopApplication();

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => call);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.StatusCode);
        var outcome = await Completed(host);
        Assert.Null(outcome.Escaped);
        Assert.Equal(503, outcome.StatusCode);
    }

    /// <summary>
    /// An OperationCanceledException with neither the client gone nor the server stopping
    /// is the operation's own failure (a timeout inside it, say), and still escapes.
    /// </summary>
    [Fact]
    public async Task OperationsOwnCancellation_StillEscapes()
    {
        await using var host = await Host.Start(HeldCallEnd.ThrowOwnCancellation);
        var run = host.CreateClient();

        await Assert.ThrowsAnyAsync<Exception>(() => run(1, TestContext.Current.CancellationToken));
        var outcome = await Completed(host);
        Assert.IsAssignableFrom<OperationCanceledException>(outcome.Escaped);
    }
}
