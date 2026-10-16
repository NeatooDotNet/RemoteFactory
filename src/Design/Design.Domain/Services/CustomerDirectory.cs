// =============================================================================
// DESIGN SOURCE OF TRUTH: [AuthorizeFactory<T>] on Interface Factories
// =============================================================================
//
// This file demonstrates custom domain authorization on an INTERFACE FACTORY:
// a customer directory, a domain service the client calls. Contrast with
// AuthorizedOrder.cs (class factory + CRUD auth).
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
// DESIGN DECISION: A domain service, not a repository (VisionAlignment D12)
//
// An interface factory is a service the client calls, named for what it does.
// A repository is never an interface factory: persistence is a [Service]
// injected into server-side operations. The directory keeps its own store,
// separate from the order store: another aggregate's facts come through that
// aggregate's own service.
//
// DESIGN DECISION: Why Execute scope (not Create/Fetch/Delete)
//
// Interface methods have no operation attribute. Adding one is an error
// (NF0106 — see CLAUDE-DESIGN.md Anti-Pattern 2 / Critical Rule 4). So the
// auth class must use scopes that apply uniformly to all interface methods:
// Execute and Read.
//
// DESIGN DECISION: Parameter matching for per-method authorization
//
// Auth methods can declare parameters typed to match interface-method
// parameters. The generator forwards matching values from the interface
// method to the auth method. Example:
//
//   interface ICustomerDirectory {
//       Task<CustomerProfile?> GetCustomer(Guid customerId);
//       Task<CustomerProfile> RenameCustomer(Guid customerId, string name);
//   }
//
//   [AuthorizeFactory(Execute)] bool CanAccessCustomer(Guid customerId);
//   // Generator forwards the Guid from GetCustomer / RenameCustomer to this auth method.
//
// This gives per-entity authorization on interface factories without
// needing operation attributes (which would violate Anti-Pattern 2).
//
// KNOWN LIMITATION: Parameterized auth methods and heterogeneous signatures
//
// If an interface factory mixes methods that have the matching parameter
// type with methods that don't, the generator emits broken code for the
// methods that lack the parameter (CS7036 at build time — a placeholder
// comment is emitted in place of the missing argument). Issue #123. This
// file keeps every method on the customer's Guid to stay within the working
// subset.
//
// DESIGN DECISION: Impl is a plain service class (no [Factory], no attributes)
//
// Critical Rule: interface factory implementations are plain service classes.
// - No [Factory] attribute on the impl class (that's for class factories)
// - No operation attributes on impl methods (Execute/Read/etc. — those are
//   for the auth class, not the impl)
// - No [Remote] on impl methods (interface IS the remote boundary)
// - Named for the interface without its I, in the same namespace, so
//   RegisterMatchingName registers it on the server.
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
// CustomerDirectory demonstrates the interface model:
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
//
// =============================================================================

using Neatoo.RemoteFactory;

namespace Design.Domain.Services;

/// <summary>
/// Authorization interface for ICustomerDirectory.
/// Demonstrates Execute/Read scopes with parameter matching for interface factories.
/// </summary>
#region skill-interface-factory-auth-authclass
public interface ICustomerDirectoryAuth
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
    /// runs on ALL four methods of ICustomerDirectory.
    /// </remarks>
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    bool HasAccess();

    /// <summary>
    /// Parameterized Execute auth — receives the customer Guid from every interface method.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: Parameter matching by type
    ///
    /// This auth method's Guid parameter matches by TYPE against the interface
    /// method's parameters. The generator forwards the Guid from each
    /// interface method into this auth method — enabling per-entity
    /// authorization (deny access to specific customers) without per-method
    /// scoping.
    ///
    /// All interface methods on ICustomerDirectory take the customer's Guid,
    /// so this auth method fires on every call.
    /// </remarks>
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    bool CanAccessCustomer(Guid customerId);

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
#endregion

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
/// The generator emits explicit services.TryAddTransient&lt;ICustomerDirectoryAuth,
/// CustomerDirectoryAuth&gt;() in the factory's service registrar. This creates a
/// static reference that survives IL trimming.
/// </remarks>
public class CustomerDirectoryAuth : ICustomerDirectoryAuth
{
    /// <summary>Controls the parameterless HasAccess() check. Default: true.</summary>
    public static bool AllowAccess { get; set; } = true;

    /// <summary>Customer that CanAccessCustomer() denies. Simulates a per-customer ACL.</summary>
    public static Guid DeniedCustomerId { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000042");

    /// <summary>Message returned by CheckReadAccess() when denied. Default: null (authorized).</summary>
    public static string? ReadDenialMessage { get; set; }

    /// <summary>
    /// Resets all flags to their defaults. Call at the start of each test.
    /// </summary>
    public static void ResetFlags()
    {
        AllowAccess = true;
        DeniedCustomerId = Guid.Parse("00000000-0000-0000-0000-000000000042");
        ReadDenialMessage = null;
    }

    public bool HasAccess() => AllowAccess;

    public bool CanAccessCustomer(Guid customerId) => customerId != DeniedCustomerId;

    public string? CheckReadAccess() => ReadDenialMessage;
}

/// <summary>
/// A customer as the directory reports it — crosses the client/server boundary.
/// </summary>
public record CustomerProfile(Guid Id, string Name, string Email);

/// <summary>
/// A contact person for a customer.
/// </summary>
public record CustomerContact(Guid CustomerId, string Name, string Email);

/// <summary>
/// Interface factory for a customer directory with custom domain authorization.
/// </summary>
/// <remarks>
/// DESIGN DECISION: [Factory] + [AuthorizeFactory&lt;T&gt;] on the interface
///
/// These two attributes together declare:
/// 1. Methods on this interface are remote entry points (from [Factory]).
/// 2. Every call is subject to authorization by ICustomerDirectoryAuth (from [AuthorizeFactory]).
///
/// DESIGN DECISION: No attributes on interface methods
///
/// Critical Rule 4: interface methods have NO attributes. The interface IS
/// the remote boundary — no [Remote] needed, no operation attribute allowed.
/// See AllPatterns.cs and CLAUDE-DESIGN.md Critical Rule 4.
///
/// GENERATOR BEHAVIOR: Factory interface and Can* methods
///
/// The generator produces:
/// - ICustomerDirectoryFactory : ICustomerDirectory — factory contract
/// - Can{Method}(Guid customerId) on the factory interface — one per interface method
///   Each Can* runs HasAccess() + CanAccessCustomer(id) + CheckReadAccess() non-throwingly.
/// - Local{Method} implementations on the server that invoke the same auth
///   checks and throw NotAuthorizedException on denial before calling
///   CustomerDirectory.
/// </remarks>
#region skill-interface-factory-auth-factory
#region attributes-factory-interface
[Factory]
[AuthorizeFactory<ICustomerDirectoryAuth>]
public interface ICustomerDirectory
#endregion
{
    /// <summary>One customer, or null. Subject to HasAccess + CanAccessCustomer(id) + CheckReadAccess.</summary>
    Task<CustomerProfile?> GetCustomer(Guid customerId);

    /// <summary>Closes a customer's account. Subject to HasAccess + CanAccessCustomer(id) + CheckReadAccess.</summary>
    /// <remarks>
    /// Returns Task: the command has no result to report. A missing customer is
    /// an exception, which reaches the client at the await.
    ///
    /// GENERATOR BEHAVIOR: A Task-returning interface method generates a Task
    /// delegate and factory method; the client proxy awaits the server call and
    /// reads no result (#122, v1.10.4).
    /// </remarks>
    Task CloseAccount(Guid customerId);

    /// <summary>Renames a customer. Subject to HasAccess + CanAccessCustomer(id) + CheckReadAccess.</summary>
    Task<CustomerProfile> RenameCustomer(Guid customerId, string name);

    /// <summary>A customer's contacts. Subject to HasAccess + CanAccessCustomer(id) + CheckReadAccess.</summary>
    Task<IReadOnlyList<CustomerContact>> ListContacts(Guid customerId);
}
#endregion

/// <summary>
/// Server-side implementation of ICustomerDirectory: a plain service.
/// </summary>
/// <remarks>
/// DID NOT DO THIS: Put [Factory] on the impl class
///
/// WRONG:
///   [Factory]  // <-- Unnecessary; duplicates the interface's factory
///   public class CustomerDirectory : ICustomerDirectory { }
///
/// RIGHT:
///   public class CustomerDirectory : ICustomerDirectory { }
///
/// The interface already carries [Factory]; the impl is a plain service.
/// </remarks>
public class CustomerDirectory : ICustomerDirectory
{
    // In real code, this would be the directory's own store
    private static readonly Dictionary<Guid, CustomerProfile> _customers = new()
    {
        [Guid.Parse("00000000-0000-0000-0000-000000000001")] = new(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Ada Customer", "ada@example.com"),
        [Guid.Parse("00000000-0000-0000-0000-000000000002")] = new(Guid.Parse("00000000-0000-0000-0000-000000000002"), "Ben Customer", "ben@example.com"),
        [Guid.Parse("00000000-0000-0000-0000-000000000003")] = new(Guid.Parse("00000000-0000-0000-0000-000000000003"), "Cy Customer", "cy@example.com"),
        [Guid.Parse("00000000-0000-0000-0000-000000000042")] = new(Guid.Parse("00000000-0000-0000-0000-000000000042"), "Denied Customer", "denied@example.com"),
    };

    private static readonly CustomerContact[] _contacts =
    [
        new(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Ada Purchasing", "purchasing@ada.example.com"),
        new(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Ada Accounts", "accounts@ada.example.com"),
    ];

    public Task<CustomerProfile?> GetCustomer(Guid customerId)
    {
        _customers.TryGetValue(customerId, out var customer);
        return Task.FromResult(customer);
    }

    public Task CloseAccount(Guid customerId)
    {
        if (!_customers.Remove(customerId))
            throw new KeyNotFoundException($"Customer {customerId} not found");
        return Task.CompletedTask;
    }

    public Task<CustomerProfile> RenameCustomer(Guid customerId, string name)
    {
        var existing = _customers.TryGetValue(customerId, out var found)
            ? found
            : throw new KeyNotFoundException($"Customer {customerId} not found");
        var renamed = existing with { Name = name };
        _customers[customerId] = renamed;
        return Task.FromResult(renamed);
    }

    public Task<IReadOnlyList<CustomerContact>> ListContacts(Guid customerId)
    {
        IReadOnlyList<CustomerContact> contacts = _contacts.Where(c => c.CustomerId == customerId).ToList();
        return Task.FromResult(contacts);
    }
}
