using Design.Domain.Aggregates;
using Design.Domain.FactoryPatterns;
using Design.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Verifies that Design.Server's composition can actually serve Design.Domain.
/// </summary>
/// <remarks>
/// <para>
/// Design.Server had no tests at all, and its registration list had drifted three
/// services behind the domain: <c>INotificationService</c>, <c>IProductReviewService</c>,
/// and <c>IPhaseAuditService</c> were injected by factory methods and registered
/// nowhere, plus <c>ITenantTokenService</c> for the constructor-injection pattern.
/// Nothing caught it because every other test resolves from
/// <see cref="TestInfrastructure.DesignClientServerContainers"/>, which has its own
/// registrations.
/// </para>
/// <para>
/// The end-to-end test below then caught a second defect the resolution checks could
/// not see: closing the gap with <c>RegisterMatchingName</c> alone registers
/// <b>transient</b>, so the factory method, each handler, and the assertion each held a
/// different <c>IPhaseAuditService</c> and the audit read back empty while every phase
/// had in fact run. Resolution tests pass under that composition; only running the
/// domain and reading the state back catches it.
/// </para>
/// <para>
/// The tests below build a container from <see cref="ServerServices.AddDesignServerServices"/>
/// — the same method <c>Program.cs</c> calls — rather than from a copy of it. That
/// distinction is the whole point: a test that re-listed the registrations would pass
/// no matter what the server did.
/// </para>
/// <para>
/// The residual, stated rather than papered over: this pins the services named below
/// and the operations exercised below. A new <c>[Service]</c> type added to
/// Design.Domain and used by no operation covered here would still be missed. Closing
/// that completely means enumerating <c>[Service]</c> parameters reflectively, which
/// this repository does not do.
/// </para>
/// </remarks>
public class DesignServerCompositionTests
{
    /// <summary>
    /// The server's composition, minus the web host: the same RemoteFactory
    /// registration call and the same server-only service seam Program.cs uses.
    /// </summary>
    private static ServiceProvider ServerComposition()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));

        services.AddNeatooAspNetCore(typeof(IOrder).Assembly);
        services.AddDesignServerServices();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Every server-only dependency Design.Domain declares resolves from the server's
    /// own container. Named one per assertion so a failure says which service is
    /// missing rather than "something in the graph".
    /// </summary>
    [Fact]
    public void ServerComposition_ResolvesEveryServerOnlyDependency()
    {
        using var provider = ServerComposition();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        // [Service] method injection — Design.Domain factory methods.
        Assert.NotNull(sp.GetRequiredService<IExampleService>());        // AllPatterns
        Assert.NotNull(sp.GetRequiredService<IExampleRepository>());     // AllPatterns (interface factory)
        Assert.NotNull(sp.GetRequiredService<IOrderRepository>());       // Order aggregate
        Assert.NotNull(sp.GetRequiredService<INotificationService>());   // handler pattern + static command
        Assert.NotNull(sp.GetRequiredService<IProductReviewService>());  // lazy-load pattern
        Assert.NotNull(sp.GetRequiredService<IPhaseAuditService>());     // phase pattern

        // Constructor injection — resolved when the factory news up the entity.
        Assert.NotNull(sp.GetRequiredService<ITenantTokenService>());
    }

    /// <summary>
    /// The framework's own phase services are present in a server composition, so a
    /// domain method taking <c>[Service] IFactoryEventPhaseCoordinator</c> resolves.
    /// These come from AddNeatooAspNetCore rather than the seam; asserting them here
    /// keeps a mode regression (they are registered for Server and Logical, not Remote)
    /// from surfacing as a mysterious failure inside a factory call.
    /// </summary>
    [Fact]
    public void ServerComposition_ResolvesThePhaseDispatchServices()
    {
        using var provider = ServerComposition();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        Assert.NotNull(sp.GetRequiredService<IFactoryEvents>());
        Assert.NotNull(sp.GetRequiredService<IFactoryEventPhaseCoordinator>());
    }

    /// <summary>
    /// Resolution is necessary but not sufficient — this runs the domain. Finalizing an
    /// invoice dispatches a handler at each of the three phases against the server's own
    /// container, exercising the audit service, the scheduler, the coordinator's drain,
    /// and the entry-call sweep together. A container that resolves every name but wires
    /// the phase services wrong fails here while passing the tests above.
    /// </summary>
    [Fact]
    public async Task ServerComposition_RunsAPhasedFactoryOperationEndToEnd()
    {
        using var provider = ServerComposition();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        var invoiceId = Guid.NewGuid();
        var factory = sp.GetRequiredService<IInvoiceFactory>();

        await factory.Finalize(invoiceId, 250m);

        var audit = sp.GetRequiredService<IPhaseAuditService>();
        Assert.Equal(
            ["invoice-immediate", "invoice-flush", "invoice-method-done", "invoice-commit"],
            audit.EntriesFor(invoiceId));
    }

    /// <summary>
    /// The order aggregate's create-and-save path against the server's composition. It
    /// method-injects both a generated child factory (<c>IOrderLineListFactory</c>) and
    /// the repository, so it fails if either the framework registration or the explicit
    /// non-conventional one is absent.
    /// </summary>
    [Fact]
    public async Task ServerComposition_RunsTheOrderAggregateSavePath()
    {
        using var provider = ServerComposition();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        var factory = sp.GetRequiredService<IOrderFactory>();
        var order = await factory.Create("composition-test");

        var saved = await factory.Save(order);

        Assert.NotNull(saved);
        Assert.Equal("composition-test", saved.CustomerName);
    }
}
