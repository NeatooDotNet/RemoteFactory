// =============================================================================
// DESIGN SOURCE OF TRUTH: Custom Domain Authorization with [AuthorizeFactory<T>]
// =============================================================================
//
// This file demonstrates the authorization INTERFACE and IMPLEMENTATION pattern
// for domain-specific authorization using [AuthorizeFactory<T>].
//
// DESIGN PATTERN: Auth interface + auth implementation
//
// The auth interface defines which operations require authorization and what
// scope each check covers. The implementation contains the actual authorization
// logic. The generator wires them together automatically.
//
// DESIGN DECISION: Both broad-scope and fine-grained operations
//
// The interface demonstrates two complementary authorization patterns:
// - Broad-scope: HasAccess() with Read | Write covers all operations
// - Fine-grained: CanCreate(), CanFetch(), CanDelete() for per-operation control
//
// When both match an operation, both must pass. For example, Create requires
// both HasAccess() (Read scope matches Create) AND CanCreate() (Create scope).
//
// DESIGN DECISION: No CanInsert() or CanUpdate() methods
//
// The broad-scope HasAccess() with Write scope already covers Insert, Update,
// and Delete. Fine-grained CanDelete() is shown separately because Delete
// often has stricter authorization than Insert/Update in real systems.
// This demonstrates that fine-grained control is opt-in per operation.
//
// DESIGN DECISION: bool return type (simplest)
//
// Auth methods can return bool, Task<bool>, string?, or Task<string?>.
// We use bool here for simplicity. Wrong return types emit diagnostic NF0202.
//
// GENERATOR BEHAVIOR: Auth type auto-registration for trimming
//
// The generator emits explicit services.TryAddTransient<IAuthorizedOrderAuth,
// AuthorizedOrderAuth>() in FactoryServiceRegistrar. This creates a static
// reference that survives IL trimming, unlike reflection-based registration.
//
// DESIGN DECISION: Static boolean flags for test configurability
//
// The implementation uses static flags so Design.Tests can toggle authorization
// per-operation without mock DI setup. Each test resets the flags it depends on.
// In production, you would inject services (e.g., IUser) via the constructor.
// See Person.DomainModel/PersonModelAuth.cs for a real-world example.
//
// DID NOT DO THIS: [Remote] on auth interface methods
//
// Auth interface methods can have [Remote], making the auth check itself
// execute on the server with server-only service dependencies. When auth
// methods are [Remote] internal, Can* methods get the IsServerRuntime guard
// and route to the server. When auth methods are public (as in this file),
// Can* methods run on the client with no guard.
// See ShowcaseAuthRemoteTests.cs for the [Remote] auth method pattern.
//
// =============================================================================

using Neatoo.RemoteFactory;

namespace Design.Domain.Aggregates;

/// <summary>
/// Authorization interface for AuthorizedOrder.
/// Defines which operations require authorization checks.
/// </summary>
public interface IAuthorizedOrderAuth
{
    /// <summary>
    /// Broad-scope check covering all Read (Create, Fetch) and Write (Insert, Update, Delete) operations.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool HasAccess();

    /// <summary>
    /// Fine-grained check for Create operations only.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    /// <summary>
    /// Fine-grained check for Fetch operations only.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    /// <summary>
    /// Fine-grained check for Delete operations only.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}

/// <summary>
/// Authorization implementation with static flags for test configurability.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Public visibility for test configurability
///
/// The implementation is public so that test projects can access the static
/// flags for per-test authorization configuration. In production, use internal
/// visibility with constructor-injected services for real authorization logic.
/// Example: internal class AuthorizedOrderAuth(IUser user) : IAuthorizedOrderAuth { ... }
/// See PersonModelAuth.cs for the real-world pattern.
/// </remarks>
public class AuthorizedOrderAuth : IAuthorizedOrderAuth
{
    /// <summary>Controls the broad-scope HasAccess() check. Default: true.</summary>
    public static bool AllowAccess { get; set; } = true;

    /// <summary>Controls the fine-grained CanCreate() check. Default: true.</summary>
    public static bool AllowCreate { get; set; } = true;

    /// <summary>Controls the fine-grained CanFetch() check. Default: true.</summary>
    public static bool AllowFetch { get; set; } = true;

    /// <summary>Controls the fine-grained CanDelete() check. Default: true.</summary>
    public static bool AllowDelete { get; set; } = true;

    /// <summary>
    /// Resets all flags to their defaults (all allowed).
    /// Call at the start of each test to avoid flag pollution.
    /// </summary>
    public static void ResetFlags()
    {
        AllowAccess = true;
        AllowCreate = true;
        AllowFetch = true;
        AllowDelete = true;
    }

    public bool HasAccess() => AllowAccess;
    public bool CanCreate() => AllowCreate;
    public bool CanFetch() => AllowFetch;
    public bool CanDelete() => AllowDelete;
}
