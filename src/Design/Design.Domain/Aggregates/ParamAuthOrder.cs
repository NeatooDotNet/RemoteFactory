// =============================================================================
// DESIGN SOURCE OF TRUTH: Parameterized [AuthorizeFactory<T>] Authorization
// =============================================================================
//
// This file demonstrates an aggregate root with parameterized authorization.
// Two features are shown:
//
// 1. Type-matched auth parameters: The auth method CanFetchOrder(Guid orderId)
//    receives the orderId from Fetch(Guid orderId), enabling per-entity access
//    control. The generator matches parameters by type.
//
// 2. Target entity auth parameters: The auth method CanWrite(IParamAuthOrder target)
//    receives the entity on write operations (Insert, Update, Delete), enabling
//    state-based authorization (e.g., deny writes to locked entities).
//
// GENERATOR BEHAVIOR: Selective CanXxx generation
//
// Because the Write auth has both non-target and target-parameterized methods:
// - CanCreate() IS generated (Read auth is parameterless)
// - CanFetch(Guid) IS generated (Fetch auth takes Guid, not target entity)
// - CanSave() IS generated (runs only non-target Write auth: CanWriteRole)
// - CanSave(target) IS generated (runs ALL Write auth: CanWriteRole + CanWrite(target))
// - CanInsert/CanUpdate/CanDelete are NOT generated (entity not available before operation)
//
// CONTRAST WITH: AuthorizedOrder.cs (parameterless auth)
//
// AuthorizedOrder has all parameterless auth methods, so the generator
// produces all Can* methods including CanSave. This entity has both
// non-target and target-parameterized Write auth, so it gets two CanSave overloads.
//
// =============================================================================

using Neatoo.RemoteFactory;

namespace Design.Domain.Aggregates;

/// <summary>
/// Public interface for ParamAuthOrder aggregate root.
/// </summary>
/// <remarks>
/// Status is exposed on the interface because the auth method
/// CanWrite(IParamAuthOrder target) needs to read it.
/// </remarks>
public interface IParamAuthOrder : IFactorySaveMeta
{
    Guid Id { get; set; }
    string CustomerName { get; set; }
    string Status { get; set; }
    new bool IsNew { get; set; }
    new bool IsDeleted { get; set; }
}

/// <summary>
/// Aggregate root demonstrating parameterized [AuthorizeFactory] authorization.
/// </summary>
[Factory]
[AuthorizeFactory<IParamAuthOrderAuth>]
internal partial class ParamAuthOrder : IParamAuthOrder, IFactorySaveMeta, IFactoryOnCompleteAsync
{
    // -------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------

    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Entity status used by CanWrite(IParamAuthOrder target) for auth decisions.
    /// When "Locked", write operations are denied by the target auth method.
    /// </summary>
    public string Status { get; set; } = "Active";

    // -------------------------------------------------------------------------
    // IFactorySaveMeta Implementation
    // -------------------------------------------------------------------------

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    // -------------------------------------------------------------------------
    // Lifecycle: Reset IsNew after Insert (same as AuthorizedOrder)
    // -------------------------------------------------------------------------

    public Task FactoryCompleteAsync(FactoryOperation factoryOperation)
    {
        if (factoryOperation == FactoryOperation.Insert)
        {
            IsNew = false;
        }
        return Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Factory Operations
    // -------------------------------------------------------------------------

    /// <summary>
    /// Create a new order. Auth: CanRead() only (no Guid param for type matching).
    /// </summary>
    [Remote, Create]
    internal void Create(string customerName)
    {
        Id = Guid.NewGuid();
        CustomerName = customerName;
        Status = "Active";
        IsNew = true;
    }

    /// <summary>
    /// Fetch an order by ID. Auth: CanRead() AND CanFetchOrder(Guid orderId).
    /// The orderId is passed to CanFetchOrder by type matching.
    /// </summary>
    [Remote, Fetch]
    internal void Fetch(Guid orderId)
    {
        Id = orderId;
        CustomerName = $"Customer_{orderId:N}";
        Status = "Active";
        IsNew = false;
    }

    /// <summary>
    /// Insert a new order. Auth: CanWrite(IParamAuthOrder target) with this entity as target.
    /// </summary>
    [Remote, Insert]
    internal Task Insert()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Update an existing order. Auth: CanWrite(IParamAuthOrder target) with this entity as target.
    /// </summary>
    [Remote, Update]
    internal Task Update()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Delete an order. Auth: CanWrite(IParamAuthOrder target) with this entity as target.
    /// </summary>
    [Remote, Delete]
    internal Task Delete()
    {
        return Task.CompletedTask;
    }
}
