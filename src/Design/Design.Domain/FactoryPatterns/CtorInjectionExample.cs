// =============================================================================
// DESIGN SOURCE OF TRUTH: Constructor Injection / Ordinal Interaction
// =============================================================================
//
// This file demonstrates how a [Factory] class with a constructor-injected
// service interacts with the serialization pipeline. The key insight: when
// the only ctor available requires non-default arguments, the generator
// SKIPS ordinal serialization for that type. Serialization still works --
// it routes through the named JSON path, which uses the DI container to
// build the instance on each side of the wire.
//
// CONTRAST WITH ExampleClassFactory (AllPatterns.cs):
//
// ExampleClassFactory declares `public ExampleClassFactory() { }` -- a
// parameterless ctor. The generator emits IOrdinalSerializable for it, and
// instances flow through ordinal arrays (compact, no reflection).
//
// CtorInjectedEntity below has no parameterless or all-default ctor. The
// generator's RequiresServiceInstantiationCheck returns true, ordinal
// emission is suppressed, and System.Text.Json falls back to the named
// path. NeatooJsonTypeInfoResolver sees the type is DI-registered (every
// [Factory] class is) and calls IServiceProvider.GetRequiredService, which
// invokes the ctor through DI -- resolving ITenantTokenService from
// whichever container is local to the current side of the wire.
//
// THE RULE IS PURELY SHAPE-BASED:
//
// The generator does not look at the [Service] attribute when deciding
// whether to emit ordinal. It only asks: "Can this type be built with no
// constructor arguments?" If yes -> ordinal. If no -> named/DI path.
//
// =============================================================================

using Neatoo.RemoteFactory;

namespace Design.Domain.FactoryPatterns;

/// <summary>
/// Per-side tenant token service. Test containers register distinct instances
/// on the client and server sides so tests can prove ctor injection runs
/// independently on each side after a wire round-trip.
/// </summary>
public interface ITenantTokenService
{
    string Token { get; }
}

/// <summary>
/// Concrete implementation of ITenantTokenService. The token value is supplied
/// at registration time and read back by entities that constructor-inject the
/// service.
/// </summary>
public class TenantTokenService : ITenantTokenService
{
    public TenantTokenService(string token)
    {
        Token = token;
    }

    public string Token { get; }
}

/// <summary>
/// Demonstrates: A [Factory] aggregate root whose constructor requires a DI
/// service. Because there is no parameterless or all-default ctor, the
/// generator does NOT emit IOrdinalSerializable for this type.
/// </summary>
/// <remarks>
/// SERIALIZATION PATH FOR THIS TYPE:
///
/// 1. Server creates the entity via DI (its ctor receives the server's
///    ITenantTokenService).
/// 2. Server serializes the entity to JSON. Properties are written; the
///    _tokens field is not serialized (fields never are).
/// 3. Client deserializes via NeatooJsonTypeInfoResolver. The resolver sees
///    that CtorInjectedEntity is a DI service (auto-registered because of
///    [Factory]) and calls ServiceProvider.GetRequiredService(typeof(...)).
///    DI invokes the ctor with the CLIENT's ITenantTokenService.
/// 4. Property setters fill Id and Name from the JSON. _tokens still holds
///    the client-side instance from step 3.
///
/// Net effect: ctor-injected services arrive populated on each side, with the
/// service instance scoped to that side's DI container. State (Id, Name) is
/// transferred from server.
///
/// COMPARE: ExampleClassFactory (AllPatterns.cs) has a parameterless ctor and
/// flows through ordinal arrays. It does not carry a ctor-injected service
/// reference.
///
/// WHEN TO PREFER THIS PATTERN:
///
/// Only when the entity genuinely needs a service reference available on both
/// sides of the wire (for example, a client-side cache lookup invoked by an
/// instance method). For server-only services, prefer method injection
/// (`[Service]` on operation method parameters), which is the common case.
/// </remarks>
[Factory]
public partial class CtorInjectedEntity
{
    private readonly ITenantTokenService _tokens;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public CtorInjectedEntity(ITenantTokenService tokens)
    {
        _tokens = tokens;
    }

    /// <summary>
    /// Entry point from client. Loads state on the server, returns the
    /// populated entity to the client.
    /// </summary>
    [Remote, Fetch]
    internal Task Fetch(int id)
    {
        Id = id;
        Name = $"Loaded_{id}";
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads the token from the ctor-injected service belonging to whichever
    /// side the instance currently lives on. Tests read this on the client to
    /// prove DI ran after deserialization.
    /// </summary>
    /// <remarks>
    /// Exposed as a property (not a method) so analyzers don't flag it; it is
    /// a pure read of the injected service's state.
    /// </remarks>
    public string TokenFromContext => _tokens.Token;
}
