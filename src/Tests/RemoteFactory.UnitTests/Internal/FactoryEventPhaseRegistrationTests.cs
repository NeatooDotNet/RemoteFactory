using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// Covers phase-aware handler registration and the DI registration of the phase queue
/// across NeatooFactory modes.
/// </summary>
public class FactoryEventPhaseRegistrationTests
{
    private sealed record RegistrationEventA(string Value) : FactoryEventBase;
    private sealed record RegistrationEventB(string Value) : FactoryEventBase;
    private sealed record RegistrationEventC(string Value) : FactoryEventBase;
    private sealed record RegistrationEventD(string Value) : FactoryEventBase;

    private sealed class HandlerOne { }
    private sealed class HandlerTwo { }

    private static Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> NoOp
        => (_, _, _, _) => Task.CompletedTask;

    [Fact]
    public void RegisterHandler_WithoutPhase_DefaultsToImmediate()
    {
        FactoryEventHandlerRegistry.RegisterHandler<RegistrationEventA>(typeof(HandlerOne), NoOp);

        var handlers = FactoryEventHandlerRegistry.GetHandlers(typeof(RegistrationEventA));

        Assert.NotNull(handlers);
        var entry = Assert.Single(handlers);
        Assert.Equal(DispatchPhase.Immediate, entry.Phase);
    }

    [Fact]
    public void RegisterHandler_WithPhase_RoundTripsThePhase()
    {
        FactoryEventHandlerRegistry.RegisterHandler<RegistrationEventB>(typeof(HandlerOne), DispatchPhase.AfterCommit, NoOp);
        FactoryEventHandlerRegistry.RegisterHandler<RegistrationEventB>(typeof(HandlerTwo), DispatchPhase.AfterFlush, NoOp);

        var handlers = FactoryEventHandlerRegistry.GetHandlers(typeof(RegistrationEventB));

        Assert.NotNull(handlers);
        Assert.Equal(2, handlers.Count);
        Assert.Contains(handlers, h => h.Phase == DispatchPhase.AfterCommit);
        Assert.Contains(handlers, h => h.Phase == DispatchPhase.AfterFlush);
    }

    [Fact]
    public void RegisterHandler_RepeatedContainerBuilds_DedupesByEventAndHandlerClass()
    {
        // Simulates multiple DI container builds in one test run.
        FactoryEventHandlerRegistry.RegisterHandler<RegistrationEventC>(typeof(HandlerOne), DispatchPhase.AfterCommit, NoOp);
        FactoryEventHandlerRegistry.RegisterHandler<RegistrationEventC>(typeof(HandlerOne), DispatchPhase.AfterCommit, NoOp);
        FactoryEventHandlerRegistry.RegisterHandler<RegistrationEventC>(typeof(HandlerOne), DispatchPhase.AfterCommit, NoOp);

        var handlers = FactoryEventHandlerRegistry.GetHandlers(typeof(RegistrationEventC));

        Assert.NotNull(handlers);
        Assert.Single(handlers);
    }

    [Fact]
    public void RegisterHandler_SameHandlerClassTwoPhases_KeepsTheFirstRegistration()
    {
        // Documents the semantics of the (eventType, handlerClassType) dedupe key: a handler
        // class declaring one event at two phases keeps the phase registered first, for the
        // life of the process. Still reachable through this public API, and still the reason
        // the generator now reports NF0504 (Warning) and skips the duplicate's entry rather
        // than emitting two registrations and letting this dedupe pick a winner silently
        // (PHASE-002). The diagnostic cannot reach a direct RegisterHandler call, so this
        // behavior stands as pinned.
        FactoryEventHandlerRegistry.RegisterHandler<RegistrationEventD>(typeof(HandlerOne), DispatchPhase.AfterCommit, NoOp);
        FactoryEventHandlerRegistry.RegisterHandler<RegistrationEventD>(typeof(HandlerOne), DispatchPhase.Immediate, NoOp);

        var handlers = FactoryEventHandlerRegistry.GetHandlers(typeof(RegistrationEventD));

        Assert.NotNull(handlers);
        var entry = Assert.Single(handlers);
        Assert.Equal(DispatchPhase.AfterCommit, entry.Phase);
    }

    [Fact]
    public void Attribute_NoArgument_DefaultsToImmediate()
    {
        var attribute = new FactoryEventHandlerAttribute<RegistrationEventA>();

        Assert.Equal(DispatchPhase.Immediate, attribute.Phase);
    }

    [Theory]
    [InlineData(DispatchPhase.Immediate)]
    [InlineData(DispatchPhase.AfterFlush)]
    [InlineData(DispatchPhase.AfterCommit)]
    public void Attribute_ExplicitPhase_RoundTrips(DispatchPhase phase)
    {
        var attribute = new FactoryEventHandlerAttribute<RegistrationEventA>(phase);

        Assert.Equal(phase, attribute.Phase);
    }

    [Theory]
    [InlineData(NeatooFactory.Server)]
    [InlineData(NeatooFactory.Logical)]
    public void PhaseDispatcher_RegisteredInModesThatDispatchHandlers(NeatooFactory mode)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(mode, typeof(FactoryEventPhaseRegistrationTests).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<IFactoryEventPhaseScheduler>());
    }

    [Fact]
    public void PhaseDispatcher_NotRegisteredInRemoteMode()
    {
        // Remote mode sends raises to the server; no handlers dispatch client-side, so
        // there is nothing to defer.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(FactoryEventPhaseRegistrationTests).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.Null(scope.ServiceProvider.GetService<IFactoryEventPhaseScheduler>());
    }

    [Fact]
    public void PhaseDispatcher_IsScoped_NotSharedAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Server, typeof(FactoryEventPhaseRegistrationTests).Assembly);

        using var provider = services.BuildServiceProvider();
        using var scopeA = provider.CreateScope();
        using var scopeB = provider.CreateScope();

        var a = scopeA.ServiceProvider.GetRequiredService<IFactoryEventPhaseScheduler>();
        var b = scopeB.ServiceProvider.GetRequiredService<IFactoryEventPhaseScheduler>();

        Assert.NotSame(a, b);

        a.Enqueue(DispatchPhase.AfterCommit, new RegistrationEventA("x"), RaiseOptions.None, NoOp);

        Assert.True(a.HasPending);
        Assert.False(b.HasPending);
    }
}
