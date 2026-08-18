using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.IntegrationTests.TestContainers;

/// <summary>
/// Pins which scope each <see cref="ClientServerContainers"/> factory returns in which
/// position.
/// </summary>
/// <remarks>
/// <para>
/// The three factories do not agree: <see cref="ClientServerContainers.Scopes()"/> and
/// <see cref="ClientServerContainers.ScopesWithLogging"/> return
/// <c>(server, client, local)</c>, while the configure-callback overload returns
/// <c>(client, server, local)</c>. Every scope is an <see cref="IServiceScope"/> and
/// tuple element names are erased at runtime, so a reorder — or a caller destructuring
/// by the wrong convention — compiles silently and hands roughly a hundred tests the
/// wrong container.
/// </para>
/// <para>
/// Aligning the odd one out would touch ~20 call sites with no compiler check on any of
/// them, so PHASE-007 pinned the orders instead of changing them. These tests are what
/// makes a future alignment safe to attempt: do it, and a missed call site shows up
/// here first.
/// </para>
/// <para>
/// Discriminators, all registration-based rather than behavioural: the client scope is
/// the only one given a <see cref="ServerServiceProvider"/>; the server scope is the
/// only one with an <see cref="IFactoryEventCollector"/> (collection exists to relay to
/// a client, so Server mode alone registers it); the local scope has neither.
/// </para>
/// </remarks>
public class ClientServerContainersOrderTests
{
    private static void AssertIsClientScope(IServiceScope scope)
    {
        Assert.NotNull(scope.ServiceProvider.GetService<ServerServiceProvider>());
        Assert.Null(scope.ServiceProvider.GetService<IFactoryEventCollector>());
    }

    private static void AssertIsServerScope(IServiceScope scope)
    {
        Assert.Null(scope.ServiceProvider.GetService<ServerServiceProvider>());
        Assert.NotNull(scope.ServiceProvider.GetService<IFactoryEventCollector>());
    }

    private static void AssertIsLocalScope(IServiceScope scope)
    {
        Assert.Null(scope.ServiceProvider.GetService<ServerServiceProvider>());
        Assert.Null(scope.ServiceProvider.GetService<IFactoryEventCollector>());
    }

    [Fact]
    public void Scopes_NoArguments_ReturnsServerClientLocal()
    {
        var (first, second, third) = ClientServerContainers.Scopes();

        AssertIsServerScope(first);
        AssertIsClientScope(second);
        AssertIsLocalScope(third);
    }

    [Fact]
    public void Scopes_WithConfigureCallbacks_ReturnsClientServerLocal()
    {
        var (first, second, third) = ClientServerContainers.Scopes(
            configureClient: null,
            configureServer: null,
            configureLocal: null);

        AssertIsClientScope(first);
        AssertIsServerScope(second);
        AssertIsLocalScope(third);
    }

    [Fact]
    public void ScopesWithLogging_ReturnsServerClientLocal()
    {
        var (first, second, third) = ClientServerContainers.ScopesWithLogging(out _);

        AssertIsServerScope(first);
        AssertIsClientScope(second);
        AssertIsLocalScope(third);
    }

    /// <summary>
    /// The relay harness wraps the divergent overload, so its own order is pinned here
    /// too rather than left to the comment at its declaration.
    /// </summary>
    [Fact]
    public void RelayTestHarness_ScopesWithRelay_ReturnsServerClientRelay()
    {
        var (server, client, relay) = RelayTestHarness.ScopesWithRelay();

        AssertIsServerScope(server);
        AssertIsClientScope(client);
        Assert.NotNull(relay);
    }
}
