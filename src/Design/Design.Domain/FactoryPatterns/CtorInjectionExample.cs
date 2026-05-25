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
// SHAPE NOTES (matching real-world entities like zNeuropathy's TreatmentContext):
//
// - The class is INTERNAL; its contract is the PUBLIC interface ICtorInjectedEntity.
// - Properties are INIT-ONLY (compatible with Neatoo aggregate patterns that
//   want immutable state after construction).
// - The factory therefore returns the INTERFACE, not the concrete type, and
//   wire deserialization routes through NeatooInterfaceJsonTypeConverter
//   before re-entering NeatooJsonTypeInfoResolver for the concrete type.
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
/// Public contract for the ctor-injected entity. Callers outside the assembly
/// see this; the concrete class is internal.
/// </summary>
public interface ICtorInjectedEntity
{
    int Id { get; init; }
    string Name { get; init; }
    string TokenFromContext { get; }
}

/// <summary>
/// Demonstrates: A [Factory] aggregate root whose constructor requires a DI
/// service. Internal class, public interface, init-only properties --
/// the trait set used by real-world entities such as zNeuropathy's
/// TreatmentContext.
/// </summary>
/// <remarks>
/// SERIALIZATION PATH FOR THIS TYPE:
///
/// 1. Server creates the entity via DI (its ctor receives the server's
///    ITenantTokenService).
/// 2. Server serializes the entity to JSON. Because the factory method
///    declares the interface return type, NeatooInterfaceJsonConverterFactory
///    writes a `$type` discriminator + nested object.
/// 3. Client deserializes the interface envelope, reads `$type`, then calls
///    JsonSerializer.Deserialize for the concrete type. That re-enters
///    NeatooJsonTypeInfoResolver, which sees CtorInjectedEntity is a DI
///    service (auto-registered because of [Factory]) and calls
///    ServiceProvider.GetRequiredService(typeof(...)). DI invokes the ctor
///    with the CLIENT's ITenantTokenService.
/// 4. Property setters fill Id and Name from the JSON. Backing fields are
///    used so that named JSON deserialization can write to them despite the
///    properties being declared init-only.
///
/// Net effect: ctor-injected services arrive populated on each side, with the
/// service instance scoped to that side's DI container. State (Id, Name) is
/// transferred from server.
/// </remarks>
[Factory]
internal partial class CtorInjectedEntity : ICtorInjectedEntity
{
    private readonly ITenantTokenService _tokens;
    private int _id;
    private string _name = string.Empty;

    public int Id
    {
        get => _id;
        init => _id = value;
    }

    public string Name
    {
        get => _name;
        init => _name = value;
    }

    public CtorInjectedEntity(ITenantTokenService tokens)
    {
        _tokens = tokens;
    }

    /// <summary>
    /// Entry point from client. Loads state on the server, returns the
    /// populated entity to the client. Uses backing-field assignment so the
    /// properties' init-only contract is respected at the language level.
    /// </summary>
    [Remote, Fetch]
    internal Task Fetch(int id)
    {
        _id = id;
        _name = $"Loaded_{id}";
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads the token from the ctor-injected service belonging to whichever
    /// side the instance currently lives on. Tests read this on the client to
    /// prove DI ran after deserialization.
    /// </summary>
    public string TokenFromContext => _tokens.Token;
}

/// <summary>
/// Public contract for the [Execute]-only ctor-injected entity. Same shape as
/// ICtorInjectedEntity, but the implementing class exposes only an [Execute]
/// operation -- no [Create]/[Fetch].
/// </summary>
public interface IExecCtorEntity
{
    int Id { get; init; }
    string Name { get; init; }
    string TokenFromContext { get; }
}

/// <summary>
/// Demonstrates the [Execute]-only variant: a [Factory] class whose only
/// operation is a static [Remote, Execute] method, with a constructor that
/// requires DI. This is the shape that exposed the registration gap in
/// FactoryModelBuilder.cs: `requiresEntityRegistration` was scoped to
/// ReadMethodModel only (Create/Fetch), so an [Execute]-only entity with
/// a DI-requiring ctor was never registered as a transient on the client.
/// Deserialization then could not resolve the type through
/// IServiceProvider.GetRequiredService and the ctor never ran.
/// </summary>
/// <remarks>
/// This entity exists to pin the registration behavior. If the generator
/// ever stops emitting `AddTransient&lt;ExecCtorEntity&gt;()` again
/// (because the [Execute]-only + RequiresServiceInstantiation case is
/// re-broken), the round-trip test for this type will fail.
/// </remarks>
[Factory]
internal partial class ExecCtorEntity : IExecCtorEntity
{
    private readonly ITenantTokenService _tokens;

    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;

    public ExecCtorEntity(ITenantTokenService tokens)
    {
        _tokens = tokens;
    }

    /// <summary>
    /// Server-side entry point. Builds a populated instance using the server's
    /// ITenantTokenService and returns it. The returned instance is serialized
    /// to the client, where deserialization must re-invoke the ctor with the
    /// client's ITenantTokenService.
    /// </summary>
    [Remote, Execute]
    public static Task<IExecCtorEntity> Open(
        int id,
        [Service] ITenantTokenService tokens)
    {
        IExecCtorEntity result = new ExecCtorEntity(tokens)
        {
            Id = id,
            Name = $"Opened_{id}",
        };
        return Task.FromResult(result);
    }

    public string TokenFromContext => _tokens.Token;
}
