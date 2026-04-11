using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events.FactoryEventHandler;

/// <summary>
/// Tests that [FactoryEventHandler] handlers run in the caller's DI scope, sequentially,
/// and with the caller's CancellationToken. These are the invariants that make
/// FactoryEvent suitable for DbContext/transaction-scoped domain events.
/// </summary>
public class FactoryEventHandlerSharedScopeTests
{
    private static (IServiceScope client, IServiceScope server, IServiceScope local) CreateScopes()
    {
        return ClientServerContainers.Scopes(
            configureClient: null,
            configureServer: null);
    }

    [Fact]
    public async Task Raise_HandlerResolvesSameScopedInstance_AsCaller()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var callerProbe = server.GetRequiredService<IScopeProbe>();

        await events.Raise(new TestScopeProbeEvent(callerProbe.InstanceId, "serverRaise"));

        // The handlers resolved IScopeProbe from their injected scope and appended
        // their names. If the caller's probe now carries "Alpha" and "Beta", the
        // handlers resolved the same instance the caller did — i.e. shared scope.
        Assert.Contains("Alpha", callerProbe.Touches);
        Assert.Contains("Beta", callerProbe.Touches);
    }

    [Fact]
    public async Task Raise_BothHandlersObserved_Sequentially()
    {
        // Sequential dispatch: when Beta runs, Alpha has already appended "Alpha"
        // to the shared probe's Touches list, so Beta records touchCount=2.
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var callerProbe = server.GetRequiredService<IScopeProbe>();
        var testService = server.GetRequiredService<IEventTestService>();

        await events.Raise(new TestScopeProbeEvent(callerProbe.InstanceId, "sequential"));

        // After Raise returns the list must contain both names (order unspecified).
        Assert.Equal(2, callerProbe.Touches.Count);
        Assert.Contains("Alpha", callerProbe.Touches);
        Assert.Contains("Beta", callerProbe.Touches);

        // Whichever ran second recorded touchCount=2. Assert at least one handler saw 2.
        var recorded = testService.GetRecordedEvents()
            .Where(e => e.EntityId == callerProbe.InstanceId)
            .ToList();
        Assert.Contains(recorded, e => e.EventName.Contains("touchCount=2"));
    }

    [Fact]
    public async Task Raise_HandlerObservesCallerMutation_ThroughSharedScopedService()
    {
        // Caller mutates the scoped probe before raising. Handlers must see the
        // mutation because they resolve the same instance.
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var callerProbe = server.GetRequiredService<IScopeProbe>();

        callerProbe.Touches.Add("Caller");

        await events.Raise(new TestScopeProbeEvent(callerProbe.InstanceId, "mutation"));

        Assert.Equal(3, callerProbe.Touches.Count);
        Assert.Equal("Caller", callerProbe.Touches[0]);
        Assert.Contains("Alpha", callerProbe.Touches);
        Assert.Contains("Beta", callerProbe.Touches);
    }

    [Fact]
    public async Task Raise_HandlerWithNonCanonicalParameterOrder_BindsCorrectly()
    {
        // TestParamOrderHandler declares parameters as (evt, CancellationToken, [Service]).
        // The generator must emit invocation arguments in declaration order — before
        // the fix, arguments were reshuffled to (evt, service, ct) and the handler
        // either failed to compile or silently bound the wrong values.
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        using var cts = new CancellationTokenSource();
        var id = Guid.NewGuid();

        await events.Raise(new TestParamOrderEvent(id), RaiseOptions.None, cts.Token);

        var recorded = testService.GetRecordedEvents();
        // The handler records ct.CanBeCanceled. A CancellationTokenSource token is
        // always cancellable (CanBeCanceled == true), so if we observe that value we
        // know the CT was wired into the correct parameter slot.
        Assert.Contains(recorded, e => e.EventName == "ParamOrderHandler:ctCancellable=True" && e.EntityId == id);
    }

    [Fact]
    public async Task Raise_CancellationToken_FlowsToHandler()
    {
        var (client, server, local) = CreateScopes();
        var events = server.GetRequiredService<IFactoryEvents>();
        var testService = server.GetRequiredService<IEventTestService>();

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        var id = Guid.NewGuid();

        // The handler awaits Task.Delay(5s, ct). Cancellation must unblock it.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => events.Raise(new TestCancellableEvent(id), RaiseOptions.None, cts.Token));

        var recorded = testService.GetRecordedEvents();
        Assert.Contains(recorded, e => e.EventName == "CancellableHandler:cancelled" && e.EntityId == id);
        Assert.DoesNotContain(recorded, e => e.EventName == "CancellableHandler:completed" && e.EntityId == id);
    }
}
