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
// DESIGN DECISION: Explicit registrations first, RegisterMatchingName after
//
// RegisterMatchingName auto-maps IFoo -> Foo across the assemblies, which covers
// most pairs for free. It matches on NAME and registers with TryAdd, so an
// explicit registration made before it is the one that stands. Any pair whose
// implementation is not the interface name minus the "I", or that must not be
// transient, gets an explicit line -- listed below with the reason, because "why
// is this one explicit" is the question a reader arrives with.
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
using Design.Domain.Services;
using Design.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using System.Security.Claims;

namespace Design.Server;

/// <summary>
/// The server-only dependencies Design.Domain's factory methods resolve.
/// </summary>
public static class ServerServices
{
    /// <summary>
    /// The server's whole service composition: RemoteFactory itself, then every
    /// server-only service Design.Domain needs.
    /// </summary>
    /// <remarks>
    /// <c>Program.cs</c> calls exactly this, and so does
    /// <c>DesignServerCompositionTests</c> — including the
    /// <see cref="ServiceCollectionExtensions.AddNeatooAspNetCore(IServiceCollection, System.Reflection.Assembly[])"/>
    /// call and its assembly argument. Keeping the framework registration inside the
    /// seam rather than in <c>Program.cs</c> is deliberate: with it outside, the test
    /// had to restate that line, and a drifting assembly argument (or a deleted
    /// registration call) would have left the test green against a server that registers
    /// nothing.
    /// </remarks>
    public static IServiceCollection AddDesignServer(this IServiceCollection services)
    {
        // -------------------------------------------------------------------
        // DESIGN DECISION: AddNeatooAspNetCore registers everything RemoteFactory needs
        //
        // Registers the generated factories of the assembly passed, in server mode,
        // with the request handler the /api/neatoo endpoint calls. The endpoint
        // itself is mapped by app.UseNeatoo() in Program.cs. The serialization
        // format is the default, Ordinal, which the client's default matches.
        //
        // COMMON MISTAKE: Forgetting to pass the assembly
        //
        // WRONG:  services.AddNeatooAspNetCore();                     // nothing registered
        // RIGHT:  services.AddNeatooAspNetCore(typeof(IOrder).Assembly);
        // -------------------------------------------------------------------
        #region readme-server-setup
        services.AddNeatooAspNetCore(typeof(IOrder).Assembly);
        #endregion

        services.AddDesignLogging();

        services.AddDesignAuthorization();

        return services.AddDesignServerServices();
    }

    /// <summary>
    /// Authentication, the policies [AspAuthorize] names, and the signed-in user.
    /// </summary>
    /// <remarks>
    /// <c>AddAuthentication()</c> registers no scheme: the application adds its own
    /// (cookies, JWT bearer, ...), and Design.Tests adds a test scheme. [AspAuthorize]
    /// names no scheme, so the policy reads <c>HttpContext.User</c>, which
    /// <c>UseAuthentication()</c> in Program.cs sets from the default scheme.
    /// <c>AspAuthorize</c> needs <c>IHttpContextAccessor</c>; without it every
    /// [AspAuthorize] call throws "No AspCoreNet Authorization registered".
    ///
    /// ICurrentUser is built from the same <c>HttpContext.User</c>, so
    /// [AuthorizeFactory] classes on the server read the user [AspAuthorize] sees.
    /// It is registered explicitly; see Design.Domain/Services/CurrentUser.cs.
    /// </remarks>
    private static IServiceCollection AddDesignAuthorization(this IServiceCollection services)
    {
        #region authorization-policy-config
        services.AddAuthentication();   // add your scheme here: cookies, JWT bearer, ...
        services.AddAuthorization(options =>
            options.AddPolicy("RequireAuthenticated", policy => policy.RequireAuthenticatedUser()));
        services.AddHttpContextAccessor();
        #endregion

        #region service-injection-httpcontext
        services.AddScoped<ICurrentUser>(sp =>
            new ClaimsUser(sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User ?? new ClaimsPrincipal()));
        #endregion

        return services;
    }

    /// <summary>
    /// The log categories RemoteFactory writes to.
    /// </summary>
    /// <remarks>
    /// Three prefixes, because RemoteFactory logs under three:
    /// - <c>Neatoo.RemoteFactory</c>: the server's request handling and event dispatch,
    ///   the serializer, the client's remote calls.
    /// - <c>RemoteFactory.Trace</c>: each request's serialize and deserialize steps.
    ///   It is NOT under the prefix above, so a filter on that prefix leaves it off.
    /// - The generated class factories, which log under their own type names, so in
    ///   the domain's namespace (<c>Design.Domain.Aggregates.OrderFactory</c>). Static
    ///   and interface factories write no entries of their own.
    /// </remarks>
    private static IServiceCollection AddDesignLogging(this IServiceCollection services)
    {
        #region aspnetcore-logging
        services.AddLogging(logging => logging
            .AddFilter("Neatoo.RemoteFactory", LogLevel.Information)
            .AddFilter("RemoteFactory.Trace", LogLevel.Warning)   // Trace: every request's (de)serialization
            .AddFilter("Design", LogLevel.Information));          // the generated class factories
        #endregion

        return services;
    }

    /// <summary>
    /// The server-only half of the composition. Private on purpose: this registers the
    /// services but NOT RemoteFactory itself, so a caller who picked it over
    /// <see cref="AddDesignServer"/> would get a container that resolves every domain
    /// dependency and no factories — the half-composition failure this seam exists to
    /// make impossible. There is exactly one entry point.
    /// </summary>
    private static IServiceCollection AddDesignServerServices(this IServiceCollection services)
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
        //
        // The in-memory stores stand in for a database, so they outlive each
        // request's scope; a real DbContext would be scoped.
        //
        // InMemoryTaxTable and InMemorySupportLog are not named for their
        // interfaces, so the convention would miss them -- on purpose: the
        // convention also runs on clients, and both are server-only.
        //
        // TenantTokenService takes a constructor argument the convention cannot
        // supply. In a real server this value comes from configuration or the
        // request's tenant claim. Whatever the SERVER registers is what a
        // [Service]-injected method sees; a Blazor client registering its own
        // instance is what the CLIENT's constructor injection sees after the entity
        // deserializes -- that contrast is the point of the CtorInjectionExample
        // pattern.
        //
        // COMMON MISTAKE: Registering only the interface of a matched pair
        //
        // RegisterMatchingName registers the concrete type too (TryAddTransient(Foo)).
        // For TenantTokenService that registration could never be built -- DI has no
        // string to pass -- and in Development, where ASP.NET Core builds every
        // registration at startup, the server would not start. Registering the
        // concrete first, built with its token, means the convention's TryAdd
        // leaves it alone; the interface forwards to it, one instance per scope.
        //
        // The convention pass covers the stateless remainder (INotificationService
        // -> NotificationService, IProductCatalog -> ProductCatalog).
        // ---------------------------------------------------------------------
        #region aspnetcore-service-registration
        services.AddScoped<IPhaseAuditService, PhaseAuditService>();
        services.AddSingleton<IOrderDbContext, InMemoryOrderDbContext>();
        services.AddScoped<IProductReviewService, InMemoryProductReviewService>();
        services.AddScoped<IExampleService, ExampleService>();
        services.AddSingleton<ITaxTable, InMemoryTaxTable>();
        services.AddSingleton<ICustomerPreferencesStore, InMemoryCustomerPreferencesStore>();
        services.AddSingleton<ISupportLog, InMemorySupportLog>();
        services.AddScoped(_ => new TenantTokenService("server-token"));
        services.AddScoped<ITenantTokenService>(sp => sp.GetRequiredService<TenantTokenService>());

        services.RegisterMatchingName(typeof(IOrder).Assembly);
        #endregion

        return services;
    }
}
