// =============================================================================
// DESIGN SOURCE OF TRUTH: [AuthorizeFactory<T>] on Interface Factories
// =============================================================================
//
// This file demonstrates custom domain authorization on an INTERFACE FACTORY.
// Contrast with AuthorizedOrder.cs (class factory + CRUD auth).
//
// DESIGN PATTERN: Bare interface + plain impl + Execute-scoped auth
//
// Interface factories don't have Create/Fetch/Insert/Update/Delete operations —
// every interface method is implicitly a remote call. Authorization uses a
// different scope model than class factories:
//
// - AuthorizeFactoryOperation.Execute  → fires on every interface method call
// - AuthorizeFactoryOperation.Read     → fires on every interface method call
//                                        (alias for broad-scope on interfaces)
//
// CRUD scopes (Create, Fetch, Insert, Update, Delete) have no effect on
// interface factories because interface methods have no CRUD mapping.
//
// DESIGN DECISION: Why Execute scope (not Create/Fetch/Delete)
//
// Interface methods have no operation attribute. Adding one is an error
// (Anti-Pattern 2 / Critical Rule 4 / NF0xxx — see CLAUDE-DESIGN.md). So the
// auth class must use scopes that apply uniformly to all interface methods:
// Execute and Read.
//
// DESIGN DECISION: Parameter matching for per-method authorization
//
// Auth methods can declare parameters typed to match interface-method
// parameters. The generator forwards matching values from the interface
// method to the auth method. Example:
//
//   interface IAuthorizedRepository {
//       Task<Item> GetItem(Guid id);
//       Task<Item> UpdateItem(Guid id, string name);
//   }
//
//   [AuthorizeFactory(Execute)] bool CanAccessItem(Guid id);
//   // Generator forwards the Guid from GetItem / UpdateItem to this auth method.
//
// This gives per-entity authorization on interface factories without
// needing operation attributes (which would violate Anti-Pattern 2).
//
// KNOWN LIMITATION: Parameterized auth methods and heterogeneous signatures
//
// If an interface factory mixes methods that have the matching parameter
// type with methods that don't, the generator currently emits broken code
// for the methods that lack the parameter (CS7036 at build time — a
// placeholder comment is emitted in place of the missing argument). A
// targeted bug fix is tracked separately; this file uses homogeneous
// signatures (every method takes a Guid id) to stay within the working
// subset. See `docs/todos/` for the bug tracking this.
//
// DESIGN DECISION: Impl is a plain service class (no [Factory], no attributes)
//
// Critical Rule: interface factory implementations are plain service classes.
// - No [Factory] attribute on the impl class (that's for class factories)
// - No operation attributes on impl methods (Execute/Read/etc. — those are
//   for the auth class, not the impl)
// - No [Remote] on impl methods (interface IS the remote boundary)
// - Standard DI registration: services.AddScoped<IAuthorizedRepository, AuthorizedRepository>()
//   on the server only.
//
// GENERATOR BEHAVIOR: Can* methods and enforcement
//
// For every interface method X, the generator emits:
// - Can{X}(matching-params) on the factory interface returning Authorized
//   — runs all applicable auth methods non-throwingly
// - Local{X} on the server — runs all applicable auth methods, throws
//   NotAuthorizedException on denial, then forwards to the impl
//
// CONTRAST WITH: AuthorizedOrder.cs (class factory)
//
// AuthorizedOrder demonstrates the CRUD model:
//   - [Create]/[Fetch]/[Insert]/[Update]/[Delete] on factory methods
//   - Auth scopes Create/Fetch/Read/Write/Delete match those operations
//   - CanCreate/CanFetch/CanSave/CanDelete generated per operation
//
// AuthorizedRepository demonstrates the interface model:
//   - No operation attributes anywhere (Anti-Pattern 2)
//   - Auth scopes Execute/Read apply uniformly
//   - Can{Method} generated per interface method (not per operation)
//   - Parameter matching gives fine-grained per-method control
//
// DID NOT DO THIS: Use Create/Fetch/Delete scopes on interface-factory auth
//
// CRUD scopes don't match interface methods (which have no CRUD operation),
// so they silently never fire. If you want per-method scoping, use parameter
// matching instead.
//
// DID NOT DO THIS: Put [Execute] or [Fetch] on the interface methods
//
// Critical Rule 4: interface factory methods need NO attributes. The generator
// emits NF0106 for any factory-operation attribute on a [Factory] interface
// method (with or without [AuthorizeFactory<T>]).
//
// DID NOT DO THIS: Put [Factory] on the impl class
//
// [Factory] on the impl would cause duplicate factory registration. The
// interface already has [Factory]; the impl is a plain service class.
// See AllPatterns.cs Anti-Pattern "Factory on Implementation Class."
//
// =============================================================================

using Neatoo.RemoteFactory;

namespace Design.Domain.Aggregates;

/// <summary>
/// Authorization interface for AuthorizedRepository.
/// Demonstrates Execute/Read scopes with parameter matching for interface factories.
/// </summary>
public interface IRepositoryAuth
{
    /// <summary>
    /// Parameterless Execute auth — fires on every interface method call.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: Execute scope on interface factories
    ///
    /// AuthorizeFactoryOperation.Execute is the broad scope for interface
    /// factories — the auth method fires on every interface method unless
    /// parameter matching restricts it. With no parameters, this method
    /// runs on ALL four methods of IAuthorizedRepository.
    /// </remarks>
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    bool HasAccess();

    /// <summary>
    /// Parameterized Execute auth — receives the Guid id from every interface method.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: Parameter matching by type
    ///
    /// This auth method's Guid parameter matches by TYPE against the interface
    /// method's parameters. The generator forwards the Guid from each
    /// interface method into this auth method — enabling per-entity
    /// authorization (deny access to specific ids) without per-method scoping.
    ///
    /// All interface methods on IAuthorizedRepository take a Guid id, so this
    /// auth method fires on every call.
    /// </remarks>
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    bool CanAccessItem(Guid id);

    /// <summary>
    /// String-returning Read auth — returns empty string on authorize, message on deny.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: String return types surface denial messages
    ///
    /// Auth methods can return bool, Task&lt;bool&gt;, string?, or Task&lt;string?&gt;.
    /// For string variants:
    /// - null or empty string = authorized
    /// - non-empty string     = denied with that string as the denial message
    ///
    /// NotAuthorizedException.Message contains the denial string, surfacing
    /// domain-specific "why" to the caller.
    ///
    /// Read scope on an interface factory behaves identically to Execute:
    /// both apply uniformly across all interface methods. Prefer Execute
    /// for interface factories for clarity (Read implies a class-factory
    /// Fetch-like semantic that doesn't exist here).
    /// </remarks>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    string? CheckReadAccess();
}

/// <summary>
/// Authorization implementation with static flags for test configurability.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Static flags for test configurability
///
/// Same pattern as AuthorizedOrderAuth and ParamAuthOrderAuth. Tests toggle
/// flags directly without DI mocking. In production, inject services
/// (IUser, IPermissionService) via constructor for real authorization logic.
///
/// GENERATOR BEHAVIOR: Auth type auto-registration for trimming
///
/// The generator emits explicit services.TryAddTransient&lt;IRepositoryAuth,
/// RepositoryAuth&gt;() in the factory's service registrar. This creates a
/// static reference that survives IL trimming.
/// </remarks>
public class RepositoryAuth : IRepositoryAuth
{
    /// <summary>Controls the parameterless HasAccess() check. Default: true.</summary>
    public static bool AllowAccess { get; set; } = true;

    /// <summary>Guid that CanAccessItem() denies. Simulates per-entity ACL.</summary>
    public static Guid DeniedItemId { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000042");

    /// <summary>Message returned by CheckReadAccess() when denied. Default: null (authorized).</summary>
    public static string? ReadDenialMessage { get; set; }

    /// <summary>
    /// Resets all flags to their defaults. Call at the start of each test.
    /// </summary>
    public static void ResetFlags()
    {
        AllowAccess = true;
        DeniedItemId = Guid.Parse("00000000-0000-0000-0000-000000000042");
        ReadDenialMessage = null;
    }

    public bool HasAccess() => AllowAccess;

    public bool CanAccessItem(Guid id) => id != DeniedItemId;

    public string? CheckReadAccess() => ReadDenialMessage;
}

/// <summary>
/// Repository data transfer record — crosses the client/server boundary.
/// </summary>
public record RepositoryItem(Guid Id, string Name, int Quantity);

/// <summary>
/// Interface factory for a repository with custom domain authorization.
/// </summary>
/// <remarks>
/// DESIGN DECISION: [Factory] + [AuthorizeFactory&lt;T&gt;] on the interface
///
/// These two attributes together declare:
/// 1. Methods on this interface are remote entry points (from [Factory]).
/// 2. Every call is subject to authorization by IRepositoryAuth (from [AuthorizeFactory]).
///
/// DESIGN DECISION: No attributes on interface methods
///
/// Critical Rule 4: interface methods have NO attributes. The interface IS
/// the remote boundary — no [Remote] needed, no operation attribute allowed.
/// See AllPatterns.cs Anti-Pattern 2 and CLAUDE-DESIGN.md Critical Rule 4.
///
/// GENERATOR BEHAVIOR: Factory interface and Can* methods
///
/// The generator produces:
/// - IAuthorizedRepositoryFactory : IAuthorizedRepository — factory contract
/// - Can{Method}(Guid id) on the factory interface — one per interface method
///   Each Can* runs HasAccess() + CanAccessItem(id) + CheckReadAccess() non-throwingly.
/// - Local{Method} implementations on the server that invoke the same auth
///   checks and throw NotAuthorizedException on denial before calling
///   AuthorizedRepository.
/// </remarks>
[Factory]
[AuthorizeFactory<IRepositoryAuth>]
public interface IAuthorizedRepository
{
    /// <summary>Fetches a single item. Subject to HasAccess + CanAccessItem(id) + CheckReadAccess.</summary>
    Task<RepositoryItem?> GetItem(Guid id);

    /// <summary>Deletes a single item. Subject to HasAccess + CanAccessItem(id) + CheckReadAccess.</summary>
    /// <remarks>
    /// KNOWN LIMITATION: Interface factory methods must return Task&lt;T&gt;, not bare Task.
    /// Bare-Task-returning interface methods produce Task&lt;Task&gt; in generated code
    /// (CS0738). Using Task&lt;bool&gt; here with a sentinel true return. Tracked as a
    /// separate bug; see `docs/todos/`.
    /// </remarks>
    Task<bool> DeleteItem(Guid id);

    /// <summary>Updates an item's name. Subject to HasAccess + CanAccessItem(id) + CheckReadAccess.</summary>
    Task<RepositoryItem> UpdateItem(Guid id, string name);

    /// <summary>Fetches a subset of items by ids. Subject to HasAccess + CanAccessItem (runs per id forwarded from the first matching Guid) + CheckReadAccess.</summary>
    Task<IReadOnlyList<RepositoryItem>> GetItems(Guid id);
}

/// <summary>
/// Server-side implementation of IAuthorizedRepository.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Plain service class — no [Factory], no attributes
///
/// Interface factory implementations are plain service classes. The interface
/// carries [Factory] and [AuthorizeFactory]; the impl carries neither.
/// DI registration is explicit:
///
///     // Server container only (impl is never registered on the client)
///     services.AddScoped&lt;IAuthorizedRepository, AuthorizedRepository&gt;();
///
/// Or use RegisterMatchingName(assembly) to auto-map IFoo → Foo by convention.
///
/// COMMON MISTAKE: Adding [Factory] to the impl
///
/// WRONG:
///   [Factory]  // Duplicate factory registration!
///   public class AuthorizedRepository : IAuthorizedRepository { }
///
/// RIGHT:
///   public class AuthorizedRepository : IAuthorizedRepository { }
///
/// The interface already carries [Factory]; the impl is a plain service.
/// </remarks>
public class AuthorizedRepository : IAuthorizedRepository
{
    private static readonly Dictionary<Guid, RepositoryItem> _items = new()
    {
        [Guid.Parse("00000000-0000-0000-0000-000000000001")] = new(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Alpha", 10),
        [Guid.Parse("00000000-0000-0000-0000-000000000002")] = new(Guid.Parse("00000000-0000-0000-0000-000000000002"), "Beta", 20),
        [Guid.Parse("00000000-0000-0000-0000-000000000042")] = new(Guid.Parse("00000000-0000-0000-0000-000000000042"), "Denied", 99),
    };

    public Task<RepositoryItem?> GetItem(Guid id)
    {
        _items.TryGetValue(id, out var item);
        return Task.FromResult(item);
    }

    public Task<bool> DeleteItem(Guid id)
    {
        var removed = _items.Remove(id);
        return Task.FromResult(removed);
    }

    public Task<RepositoryItem> UpdateItem(Guid id, string name)
    {
        var updated = new RepositoryItem(id, name, _items.TryGetValue(id, out var existing) ? existing.Quantity : 0);
        _items[id] = updated;
        return Task.FromResult(updated);
    }

    public Task<IReadOnlyList<RepositoryItem>> GetItems(Guid id)
    {
        IReadOnlyList<RepositoryItem> result = _items.TryGetValue(id, out var item) ? [item] : [];
        return Task.FromResult(result);
    }
}
