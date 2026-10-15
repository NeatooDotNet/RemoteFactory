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
// DESIGN DECISION: The rules read the signed-in user
//
// AuthorizedOrderAuth takes ICurrentUser by constructor. Tests deny an
// operation by taking a role from the test user, the way a real user lacks one.
// (It used to read static flags, which tied tests together across classes
// running in parallel.)
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

using Design.Domain.Services;
using Neatoo.RemoteFactory;

namespace Design.Domain.Aggregates;

/// <summary>
/// Authorization interface for AuthorizedOrder.
/// Defines which operations require authorization checks.
/// </summary>
#region authorization-interface
public interface IAuthorizedOrderAuth
{
    /// <summary>
    /// Broad-scope check covering all Read (Create, Fetch) and Write (Insert, Update, Delete) operations.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool HasAccess();

    /// <summary>
    /// Who may create an order: checked by the local Create (and CanCreate) on
    /// the client, and again on the server when the new order is saved (Insert).
    /// </summary>
    /// <remarks>
    /// A local Create runs only on the client, so the client check is for the
    /// screen; the Insert scope is what the server enforces.
    /// </remarks>
    [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Insert)]
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
#endregion


/// <summary>
/// The roles AuthorizedOrder's rules name.
/// </summary>
public static class OrderRoles
{
    public const string Sales = "Sales";
    public const string Support = "Support";
    public const string Manager = "Manager";
}

/// <summary>
/// AuthorizedOrder's rules, read from the signed-in user.
/// </summary>
/// <remarks>
/// Its methods are public and take only ICurrentUser, which both tiers
/// register, so Can* and the local Create run these rules on the client with no
/// server call. The server runs them for Fetch and Save: Insert re-checks
/// CanCreate, so a client cannot skip it.
/// </remarks>
#region authorization-implementation
public class AuthorizedOrderAuth(ICurrentUser user) : IAuthorizedOrderAuth
{
    public bool HasAccess() => user.IsAuthenticated;

    public bool CanCreate() => user.IsInRole(OrderRoles.Sales);

    public bool CanFetch() => user.IsInRole(OrderRoles.Sales) || user.IsInRole(OrderRoles.Support);

    public bool CanDelete() => user.IsInRole(OrderRoles.Manager);
}
#endregion
