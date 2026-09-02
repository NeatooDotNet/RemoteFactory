using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// Relay dispatch in the PRODUCTION <see cref="MakeRemoteDelegateRequest"/>, for both call
/// paths — the <c>[Remote]</c> factory call and the client-initiated
/// <see cref="IFactoryEvents.Raise{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why these are unit tests and not integration tests.</b> The integration suite
/// substitutes <c>MakeSerializedServerStandinDelegateRequest</c> for
/// <see cref="IMakeRemoteDelegateRequest"/> to avoid HTTP, which replaces the entire class
/// under test. Those tests pin the end-to-end contract, but they cannot see a defect that
/// lives in the production client — and one did: <c>ForDelegateEvent</c> awaited the
/// round-trip and discarded the <see cref="RemoteResponseDto"/>, so a client-initiated
/// <c>Raise</c> never relayed anything. The production class had no direct test at all,
/// which is how that survived.
/// </para>
/// <para>
/// Only the HTTP boundary is stubbed here — <see cref="MakeRemoteDelegateRequestHttpCall"/>
/// is a delegate, so the real class, the real serializer and the real relay wiring are all
/// exercised.
/// </para>
/// </remarks>
public class MakeRemoteDelegateRequestRelayTests
{
    /// <summary>Event type used only by this class; the registry resolves it by assembly scan.</summary>
    public record UnitRelayProbeEvent(Guid Id) : FactoryEventBase;

    private sealed class RecordingRelay : IFactoryEventRelay
    {
        private readonly ConcurrentQueue<FactoryEventBase> _received = new();
        private int _invocationCount;

        public int InvocationCount => Volatile.Read(ref _invocationCount);
        public IReadOnlyList<FactoryEventBase> Received => _received.ToArray();

        public Task Relay(IReadOnlyList<FactoryEventBase> events)
        {
            Interlocked.Increment(ref _invocationCount);
            foreach (var evt in events)
            {
                _received.Enqueue(evt);
            }
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Builds a Remote-mode container whose only stub is the HTTP call, which returns
    /// <paramref name="relayed"/> as the response's relay batch.
    /// </summary>
    private static ServiceProvider BuildClient(RecordingRelay relay, params FactoryEventBase[] relayed)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(MakeRemoteDelegateRequestRelayTests).Assembly);
        services.AddSingleton<IFactoryEventRelay>(relay);

        // Last-writer-wins over the real HttpClient-backed registration.
        services.AddScoped<MakeRemoteDelegateRequestHttpCall>(sp => (_, _) =>
        {
            var serializer = sp.GetRequiredService<INeatooJsonSerializer>();
            var events = relayed.Length == 0
                ? null
                : relayed
                    .Select(e => new RelayedFactoryEvent
                    {
                        TypeFullName = e.GetType().FullName!,
                        Json = serializer.Serialize(e, e.GetType()) ?? "{}",
                    })
                    .ToList();

            return Task.FromResult(new RemoteResponseDto("null", events));
        });

        return services.BuildServiceProvider();
    }

    private static async Task WaitForAsync(Func<bool> predicate, string description)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!predicate())
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"Timed out waiting for: {description}. Relay dispatch is fire-and-forget, " +
                    "so this means it never arrived — not that it arrived late.");
            }
            await Task.Delay(5);
        }
    }

    /// <summary>
    /// The regression test for the client-raise relay gap. A client-initiated
    /// <c>Raise</c> must deliver the response's relay batch, exactly as a factory call does.
    /// </summary>
    [Fact]
    public async Task ForDelegateEvent_RelaysTheResponseBatch()
    {
        var id = Guid.NewGuid();
        var relay = new RecordingRelay();
        using var provider = BuildClient(relay, new UnitRelayProbeEvent(id));
        using var scope = provider.CreateScope();

        var events = scope.ServiceProvider.GetRequiredService<IFactoryEvents>();
        await events.Raise(new UnitRelayProbeEvent(id));

        await WaitForAsync(
            () => relay.Received.Count > 0,
            "the relay to receive the client-raise response batch");

        var received = Assert.IsType<UnitRelayProbeEvent>(Assert.Single(relay.Received));
        Assert.Equal(id, received.Id);
    }

    /// <summary>
    /// The empty-batch case still produces exactly one invocation, matching the factory
    /// path's documented "one round-trip = one Relay call, even when empty".
    /// </summary>
    [Fact]
    public async Task ForDelegateEvent_EmptyBatch_RelayInvokedOnce()
    {
        var relay = new RecordingRelay();
        using var provider = BuildClient(relay);
        using var scope = provider.CreateScope();

        var events = scope.ServiceProvider.GetRequiredService<IFactoryEvents>();
        await events.Raise(new UnitRelayProbeEvent(Guid.NewGuid()));

        await WaitForAsync(() => relay.InvocationCount == 1, "the single Relay invocation");
        Assert.Empty(relay.Received);
    }

    /// <summary>
    /// The factory-call path keeps its existing behavior. Guards the extraction of the
    /// shared dispatch helper — it must not perturb the path that already worked.
    /// </summary>
    [Fact]
    public async Task ForDelegateNullable_StillRelaysTheResponseBatch()
    {
        var id = Guid.NewGuid();
        var relay = new RecordingRelay();
        using var provider = BuildClient(relay, new UnitRelayProbeEvent(id));
        using var scope = provider.CreateScope();

        var request = scope.ServiceProvider.GetRequiredService<IMakeRemoteDelegateRequest>();
        await request.ForDelegateNullable<object>(typeof(RaiseFactoryEventRemote), [], CancellationToken.None);

        await WaitForAsync(
            () => relay.Received.Count > 0,
            "the relay to receive the factory-call response batch");

        var received = Assert.IsType<UnitRelayProbeEvent>(Assert.Single(relay.Received));
        Assert.Equal(id, received.Id);
    }
}
