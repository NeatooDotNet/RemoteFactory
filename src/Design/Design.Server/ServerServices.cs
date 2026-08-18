// =============================================================================
// DESIGN SOURCE OF TRUTH: Server-Only Service Registration
// =============================================================================
//
// Every service Design.Domain injects with [Service] on a factory method, plus the
// one it injects through a constructor. If a type is missing here, the factory
// method that needs it throws at resolution time on the server -- and only when a
// client actually calls it, which is why this file exists as a seam a test can
// call rather than as inline lines in Program.cs.
//
// DESIGN DECISION: RegisterMatchingName first, explicit registrations after
//
// RegisterMatchingName auto-maps IFoo -> Foo across the assembly, which covers
// most pairs for free. It matches on NAME, so any pair whose implementation is
// not the interface name minus the "I" needs an explicit line -- listed below
// with the reason, because "why is this one explicit" is the question a reader
// arrives with.
//
// COMMON MISTAKE: Registering services only in the test container
//
// The test harness (Design.Tests) has its own container and it is easy to add a
// service there when a test needs it and never notice the real server is missing
// it. DesignServerCompositionTests calls THIS method for exactly that reason:
// the check runs against the server's registration list, not a copy of it.
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Domain.FactoryPatterns;
using Neatoo.RemoteFactory;

namespace Design.Server;

/// <summary>
/// The server-only dependencies Design.Domain's factory methods resolve.
/// </summary>
public static class ServerServices
{
    /// <summary>
    /// Registers every server-only service Design.Domain needs, in the composition
    /// order a real server would use.
    /// </summary>
    /// <remarks>
    /// Called by <c>Program.cs</c> at startup and by <c>DesignServerCompositionTests</c>,
    /// which is what makes the composition testable without standing up the web host.
    /// </remarks>
    public static IServiceCollection AddDesignServerServices(this IServiceCollection services)
    {
        // ---------------------------------------------------------------------
        // DESIGN DECISION: Explicit AddScoped for anything that holds state
        //
        // RegisterMatchingName registers TRANSIENT (TryAddTransient). That is the
        // right default for a stateless mapper or a thin wrapper, and the wrong one
        // for anything a factory method and its handlers must SHARE within one
        // operation: each [Service] injection point would get its own instance.
        //
        // COMMON MISTAKE: Letting the convention register a stateful service
        //
        // The failure is quiet. The operation succeeds, every handler runs, and the
        // state each one wrote is simply somewhere else -- an audit sink that reads
        // back empty, a repository whose Insert is invisible to the next read, a
        // unit-of-work marker that never sees the work. Register those explicitly,
        // as scoped, and let the convention pass below pick up the rest.
        //
        // Registered before RegisterMatchingName on purpose: the convention uses
        // TryAdd*, so an explicit registration made first is the one that stands.
        // ---------------------------------------------------------------------
        services.AddScoped<IPhaseAuditService, PhaseAuditService>();
        services.AddScoped<IOrderRepository, InMemoryOrderRepository>();
        services.AddScoped<IProductReviewService, InMemoryProductReviewService>();
        services.AddScoped<IExampleService, ExampleService>();

        // Explicit for a different reason: the implementation takes a constructor
        // argument the convention cannot supply. In a real server this value comes
        // from configuration or the request's tenant claim. Whatever the SERVER
        // registers is what a [Service]-injected method sees; a Blazor client
        // registering its own instance is what the CLIENT's constructor injection
        // sees after the entity deserializes -- that contrast is the point of the
        // CtorInjectionExample pattern.
        services.AddScoped<ITenantTokenService>(_ => new TenantTokenService("server-token"));

        // Convention pass for the stateless remainder (INotificationService ->
        // NotificationService, IExampleRepository -> ExampleRepository) and for the
        // IFoo -> Foo pairs the generated factories expect to resolve.
        services.RegisterMatchingName(typeof(IOrder).Assembly);

        return services;
    }
}
