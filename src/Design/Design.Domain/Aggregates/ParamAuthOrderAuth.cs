// =============================================================================
// DESIGN SOURCE OF TRUTH: Parameterized Authorization with [AuthorizeFactory<T>]
// =============================================================================
//
// This file demonstrates PARAMETERIZED authorization methods -- auth methods
// that receive arguments matched by type from factory method parameters or
// the target entity itself.
//
// DESIGN PATTERN: Two forms of parameterized auth
//
// 1. Type-matched parameters: Auth method declares a parameter whose type
//    matches a factory method parameter. The generator passes the matching
//    value to the auth method. Example: CanFetch(Guid orderId) receives the
//    orderId from Fetch(Guid orderId).
//
// 2. Target parameter: Auth method declares a parameter whose type matches
//    the entity type (class or interface). On write operations (Insert,
//    Update, Delete), the generator passes the target entity so the auth
//    method can inspect entity state. Example: CanWrite(IParamAuthOrder target)
//    receives the entity being saved.
//
// DESIGN DECISION: Type-based matching (not name-based)
//
// Parameters are matched by type, not by name. The auth method parameter name
// can differ from the factory method parameter name -- only the type matters.
// When multiple parameters share the same type, they're matched in order.
//
// DESIGN DECISION: Target parameters suppress CanInsert/CanUpdate/CanDelete but NOT CanSave
//
// When an auth method has a target parameter (matching the entity type),
// CanInsert/CanUpdate/CanDelete are NOT generated on the factory interface.
// These Can* methods run before the write operation when the entity isn't available.
//
// CanSave IS generated with TWO overloads:
// - CanSave() parameterless: runs only non-target Write auth methods (role checks)
// - CanSave(target): runs ALL Write auth methods (both non-target and target-parameterized)
// The caller has the entity in hand when deciding whether to save, so the target
// parameter can be passed through. This follows the same pattern as CanFetch(Guid)
// calling both CanRead() and CanFetchOrder(Guid).
//
// CanCreate/CanFetch ARE still generated because Read auth doesn't use
// the target parameter.
//
// CONTRAST WITH: AuthorizedOrderAuth.cs (parameterless auth)
//
// AuthorizedOrderAuth demonstrates parameterless auth methods that don't
// receive any arguments. That auth class generates all Can* methods
// (CanCreate, CanFetch, CanSave, CanDelete). This auth class generates
// CanCreate, CanFetch, and CanSave (with both overloads) because Write
// auth has both non-target and target-parameterized methods.
//
// =============================================================================

using Neatoo.RemoteFactory;

namespace Design.Domain.Aggregates;

/// <summary>
/// Parameterized authorization interface for ParamAuthOrder.
/// Demonstrates type-matched parameters and target entity parameters.
/// </summary>
public interface IParamAuthOrderAuth
{
    /// <summary>
    /// Parameterless Read auth covering Create operations.
    /// </summary>
    /// <remarks>
    /// Read scope matches Create and Fetch. For Fetch, both this method
    /// AND CanFetchOrder(Guid) are checked -- both must pass.
    /// For Create, only this method is checked (Create has no Guid param).
    /// </remarks>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();

    /// <summary>
    /// Type-matched Fetch auth -- Guid parameter matched to Fetch(Guid orderId).
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: Type-based parameter matching
    ///
    /// The Guid parameter type matches the factory's Fetch(Guid orderId) parameter.
    /// The generator passes the orderId value to this method at runtime.
    /// This enables per-entity authorization: deny access to specific orders.
    ///
    /// Fetch scope (not Read scope) ensures this only applies to Fetch,
    /// not Create. Create has no Guid parameter, so a Read-scoped
    /// parameterized method would fail to match.
    /// </remarks>
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetchOrder(Guid orderId);

    /// <summary>
    /// Non-target Write auth -- parameterless role/permission check.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: CanSave generation with two overloads
    ///
    /// Because both a non-target Write method (this) and a target Write method
    /// (CanWrite below) exist, the generator produces TWO CanSave overloads:
    /// - CanSave() parameterless: runs only this method (non-target auth)
    /// - CanSave(target): runs BOTH this method AND CanWrite(target)
    ///
    /// CanInsert/CanUpdate/CanDelete remain suppressed because they run
    /// before the write operation when the entity isn't available.
    /// </remarks>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWriteRole();

    /// <summary>
    /// Target parameter Write auth -- receives the entity being saved.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: Target parameter detection
    ///
    /// The IParamAuthOrder parameter type matches the entity type.
    /// On write operations (Insert, Update, Delete), the generator passes
    /// the target entity so auth can inspect entity state (e.g., Status).
    ///
    /// GENERATOR BEHAVIOR: CanSave with target parameter
    ///
    /// CanSave(target) IS generated on the factory interface. The caller has
    /// the entity in hand when deciding whether to save, so the target parameter
    /// can be passed through. CanSave(target) calls ALL Write-scoped auth methods:
    /// both CanWriteRole() (non-target) and CanWrite(target) (target-parameterized).
    ///
    /// CanInsert/CanUpdate/CanDelete are NOT generated because these Can* methods
    /// run before the write operation when the entity isn't available.
    /// </remarks>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite(IParamAuthOrder target);
}

/// <summary>
/// Parameterized authorization implementation with static flags for testing.
/// </summary>
/// <remarks>
/// Same test-configurability pattern as AuthorizedOrderAuth. Static flags
/// control auth decisions per-test. In production, use constructor-injected
/// services (IUser, IPermissionService) for real authorization logic.
/// </remarks>
public class ParamAuthOrderAuth : IParamAuthOrderAuth
{
    /// <summary>
    /// Specific Guid that CanFetchOrder will deny.
    /// Simulates per-entity access control (e.g., order belongs to another user).
    /// </summary>
    public static Guid DenyFetchGuid { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>Controls the parameterless CanRead() check. Default: true.</summary>
    public static bool AllowRead { get; set; } = true;

    /// <summary>Controls the parameterless CanWriteRole() check. Default: true.</summary>
    public static bool AllowWriteRole { get; set; } = true;

    /// <summary>
    /// Resets all flags to their defaults (all allowed).
    /// Call at the start of each test to avoid flag pollution.
    /// </summary>
    public static void ResetFlags()
    {
        AllowRead = true;
        AllowWriteRole = true;
        DenyFetchGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    public bool CanRead() => AllowRead;

    /// <summary>
    /// Non-target Write auth -- simulates a role/permission check.
    /// In production, this would check user roles or permissions.
    /// </summary>
    public bool CanWriteRole() => AllowWriteRole;

    /// <summary>
    /// Denies fetch when orderId matches DenyFetchGuid.
    /// In production, this would check whether the current user has access
    /// to the specific order (e.g., query a permissions table).
    /// </summary>
    public bool CanFetchOrder(Guid orderId) => orderId != DenyFetchGuid;

    /// <summary>
    /// Denies writes when the target entity's Status is "Locked".
    /// In production, this would check entity state against business rules
    /// (e.g., locked records can't be modified, approved orders can't be edited).
    /// </summary>
    public bool CanWrite(IParamAuthOrder target) => target.Status != "Locked";
}
