// =============================================================================
// DESIGN SOURCE OF TRUTH: Testing RemoteFactory Over Real HTTP
// =============================================================================
//
// DesignClientServerContainers runs the server in-process: it serializes every
// remote call and hands it to the server's handler, but no HTTP happens, so no
// middleware runs and there are no status codes, headers or request aborts. The
// tests in this folder host Design.Server's real Program with
// WebApplicationFactory and send a Remote-mode client's calls to it over HTTP.
// They cover what only HTTP shows; domain behaviour stays with the harness.
//
// DESIGN DECISION: One host for the whole test run
//
// UseNeatoo() hands the host's ILoggerFactory to a process-wide static that the
// ordinal serializer reads. A host disposed mid-run would leave that static
// pointing at a disposed factory while harness tests keep serializing. The
// host is an assembly fixture: built once, disposed after every test has run.
//
// DESIGN DECISION: Stand-ins only for what a test must control
//
// The host runs the real configuration and services. Two are replaced, each a
// stand-in for something outside the server:
// - the order store is wrapped by one that can hold a read open until the
//   caller cancels (a slow database);
// - the AuthorizedOrder rules come from an instance the tests switch, not
//   from AuthorizedOrderAuth's static flags, which the harness's
//   AuthorizationTests switch in parallel.
//
// =============================================================================

using System.Collections.Concurrent;
using Design.Domain.Aggregates;
using Design.Tests.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;

[assembly: AssemblyFixture(typeof(DesignHttpHost))]

namespace Design.Tests.Http;

/// <summary>
/// Design.Server's Program, hosted in memory for the whole test run.
/// </summary>
public sealed class DesignHttpHost : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// Starts the server before any test runs. WebApplicationFactory otherwise starts
    /// it on first use, and two test classes running in parallel can each start one:
    /// a request then lands on a host whose services the test is not reading.
    /// </summary>
    public ValueTask InitializeAsync()
    {
        _ = Server;   // reading Server builds and starts the host
        return ValueTask.CompletedTask;
    }

    /// <summary>Every log entry the server wrote, by category.</summary>
    public CapturedLogs Logs { get; } = new();

    /// <summary>The store wrapper whose reads a test can hold open.</summary>
    public GatedOrderDbContext Store { get; } = new(new InMemoryOrderDbContext());

    /// <summary>The AuthorizedOrder rules the server applies.</summary>
    public SwitchableOrderAuth Auth { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IOrderDbContext>();
            services.AddSingleton<IOrderDbContext>(Store);
            services.RemoveAll<IAuthorizedOrderAuth>();
            services.AddSingleton<IAuthorizedOrderAuth>(Auth);
        });
    }

    /// <summary>
    /// A client container registered the way Design.Client.Blazor's Program
    /// registers, with its HttpClient pointed at this host.
    /// </summary>
    public IServiceScope CreateClientScope(params DelegatingHandler[] handlers)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IOrder).Assembly);
        services.AddKeyedScoped(
            RemoteFactoryServices.HttpClientKey,
            (sp, key) => CreateDefaultClient(handlers));
        return services.BuildServiceProvider().CreateScope();
    }
}

/// <summary>
/// Records each response the client receives, so a test can read its headers.
/// </summary>
public sealed class ResponseRecorder : DelegatingHandler
{
    public HttpResponseMessage? LastResponse { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastResponse = await base.SendAsync(request, cancellationToken);
        return LastResponse;
    }
}

/// <summary>
/// The order store, with reads of one order id that wait for the caller's
/// cancellation: a stand-in for a slow database query.
/// </summary>
public sealed class GatedOrderDbContext(IOrderDbContext inner) : IOrderDbContext
{
    private readonly ConcurrentDictionary<int, ReadHold> held = new();

    /// <summary>
    /// The next read of <paramref name="orderId"/> waits until its token is cancelled.
    /// </summary>
    public ReadHold HoldNextReadOf(int orderId) => held.GetOrAdd(orderId, _ => new ReadHold());

    public async Task<OrderRow?> FindAsync(int id, CancellationToken cancellationToken = default)
    {
        if (held.TryRemove(id, out var hold))
        {
            hold.Enter();
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            finally
            {
                hold.Release(cancellationToken);
            }
        }
        return await inner.FindAsync(id, cancellationToken);
    }

    public Task<OrderRow?> FindDraftAsync(string customerName, CancellationToken cancellationToken = default)
        => inner.FindDraftAsync(customerName, cancellationToken);

    public Task<OrderLineRow?> FindLineAsync(Guid lineId, CancellationToken cancellationToken = default)
        => inner.FindLineAsync(lineId, cancellationToken);

    public void Add(OrderRow row) => inner.Add(row);

    public void Remove(OrderRow row) => inner.Remove(row);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => inner.SaveChangesAsync(cancellationToken);
}

/// <summary>
/// One held read: when the store began it, and the token it ended with.
/// </summary>
public sealed class ReadHold
{
    private readonly TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<CancellationToken> released = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes when the server's read reaches the store.</summary>
    public Task Entered => entered.Task;

    /// <summary>Completes, with the read's token, when the read stops waiting.</summary>
    public Task<CancellationToken> Released => released.Task;

    internal void Enter() => entered.TrySetResult();

    internal void Release(CancellationToken token) => released.TrySetResult(token);
}

/// <summary>
/// AuthorizedOrder's rules, switched per test on this instance.
/// </summary>
public sealed class SwitchableOrderAuth : IAuthorizedOrderAuth
{
    public bool AllowAccess { get; set; } = true;
    public bool AllowFetch { get; set; } = true;

    public bool HasAccess() => AllowAccess;
    public bool CanCreate() => true;
    public bool CanFetch() => AllowFetch;
    public bool CanDelete() => true;
}

/// <summary>
/// A logger provider that keeps the category of every entry written.
/// </summary>
public sealed class CapturedLogs : ILoggerProvider
{
    private readonly ConcurrentQueue<(string Category, LogLevel Level, string Message)> entries = new();

    public IReadOnlyList<(string Category, LogLevel Level, string Message)> Entries => entries.ToArray();

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName, entries);

    public void Dispose() { }

    private sealed class Logger(string category, ConcurrentQueue<(string, LogLevel, string)> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => entries.Enqueue((category, logLevel, formatter(state, exception)));
    }
}
