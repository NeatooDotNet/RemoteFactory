// =============================================================================
// DESIGN SOURCE OF TRUTH: Aggregate Root with Lifecycle Hooks
// =============================================================================
//
// This file demonstrates the CLASS FACTORY pattern for an aggregate root,
// including all lifecycle hooks and the IFactorySaveMeta interface.
//
// DESIGN PATTERN: Internal class with public interface
//
// The aggregate root class is `internal` with a `public interface IOrder`.
// The generator detects the matching interface by naming convention
// (class Order -> interface IOrder) and uses IOrder in the generated
// factory interface signatures:
//
//   public interface IOrderFactory {
//       IOrder Create(string customerName, ...);       // local
//       Task<IOrder?> Fetch(int id, ...);              // [Remote]; null when absent
//       Task<IOrder> OpenDraft(string customerName);   // [Remote] Execute: Fetch, else Create
//       Task<IOrder?> Save(IOrder target, ...);        // [Remote] Insert/Update/Delete
//   }
//
// This enables IL trimming of the concrete Order class on the client
// while keeping the public IOrder interface accessible everywhere.
//
// =============================================================================

using Design.Domain.Entities;
using Design.Domain.ValueObjects;
using Neatoo.RemoteFactory;

namespace Design.Domain.Aggregates;

/// <summary>
/// Public interface for Order aggregate root.
/// Exposed to callers outside the assembly (e.g., Blazor client).
/// </summary>
#region order-interface
public interface IOrder : IFactorySaveMeta
{
    int Id { get; set; }
    string CustomerName { get; set; }
    OrderStatus Status { get; set; }
    IOrderLineList Lines { get; set; }
    Money Total { get; }
    void AddLine(IOrderLine line);
    void RemoveLine(IOrderLine line);
    bool CanSubmit { get; }
    void Submit();
    void Cancel();
    void MarkDeleted();
}
#endregion

/// <summary>
/// Order aggregate root demonstrating lifecycle hooks and Save routing.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Internal class with public interface
///
/// The class is `internal` so the generator produces factory interface
/// methods that use `IOrder` (the public interface) instead of `Order`.
/// This enables:
/// 1. IL trimming of the concrete class on Blazor WASM clients
/// 2. Clean API surface -- callers depend on the interface, not the impl
/// 3. Consistent pattern with child entities (OrderLine, OrderLineList)
///
/// DESIGN DECISION: Aggregate roots implement IFactorySaveMeta for Save routing
///
/// The factory's Save() method examines IsNew and IsDeleted to determine
/// which operation to call:
/// - IsNew=true, IsDeleted=false -> Insert
/// - IsNew=false, IsDeleted=false -> Update
/// - IsNew=false, IsDeleted=true -> Delete
/// - IsNew=true, IsDeleted=true -> No operation (new item deleted before save)
///
/// GENERATOR BEHAVIOR: For IFactorySaveMeta implementations, the generator
/// creates a Save() method on the factory that routes to Insert/Update/Delete
/// based on the IsNew and IsDeleted properties.
/// </remarks>
#region order-class-declaration
[Factory]
internal partial class Order : IOrder, IFactorySaveMeta, IFactoryOnStartAsync, IFactoryOnCompleteAsync
#endregion
{
    // -------------------------------------------------------------------------
    // Properties
    //
    // GENERATOR BEHAVIOR: The ordinal format writes these as one JSON array, in
    // ordinal name order (CustomerName, Id, IsDeleted, IsNew, Lines, Status), not
    // in declaration order. Total has no setter, so it is not written; the other
    // side computes it. Lines is an interface, so it is written with its concrete
    // type: {"$type": "...OrderLineList", "$value": [...]}. SerializationTests
    // pins both.
    // -------------------------------------------------------------------------

    #region serialization-ordinal-versioning
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public IOrderLineList Lines { get; set; } = null!;
    public Money Total => Lines?.CalculateTotal() ?? Money.Zero;
    #endregion

    // -------------------------------------------------------------------------
    // IFactorySaveMeta Implementation
    //
    // DESIGN DECISION: IsNew and IsDeleted control Save routing
    //
    // These properties let the generated factory know which persistence
    // operation to perform. The entity controls its own state; the factory
    // just reads it.
    //
    // DID NOT DO THIS: Have the factory track entity state
    //
    // Reasons:
    // 1. Entity owns its state - single source of truth
    // 2. Enables complex state transitions (e.g., soft delete + undo)
    // 3. Works with detached scenarios (entity saved, modified, saved again)
    // -------------------------------------------------------------------------

    #region save-ifactorysavemeta
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }
    #endregion

    // -------------------------------------------------------------------------
    // Lifecycle Hooks
    //
    // DESIGN DECISION: Async-first lifecycle hooks
    //
    // Both sync and async versions are available:
    // - IFactoryOnStart / IFactoryOnStartAsync
    // - IFactoryOnComplete / IFactoryOnCompleteAsync
    // - IFactoryOnCancelled / IFactoryOnCancelledAsync
    //
    // Use async versions when you need to await operations in hooks.
    // The generator calls the appropriate version based on what's implemented.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called before each of Order's Task-returning operations begins.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: the async start hook runs before a Task-returning
    /// operation: here Fetch, FetchDraft, Insert, Update and Delete. It does not run
    /// for the synchronous Create, and no start hook runs for a constructor or
    /// static Create/Fetch or for a class-level [Execute] such as OpenDraft.
    /// CustomerPreferences shows a start hook doing work.
    ///
    /// Use cases:
    /// - Logging or auditing the operation
    /// - Setup the operation needs (timestamps, correlation)
    ///
    /// DID NOT DO THIS: Validate in the hook and throw to stop the operation
    ///
    /// A check the user can recover from is a verdict the UI reads before
    /// it acts (a CanX or a result), never a throw in a factory method or a
    /// hook. Authorization is [AuthorizeFactory<T>]. A throw is for broken
    /// state (VisionAlignment R-E).
    /// </remarks>
    public Task FactoryStartAsync(FactoryOperation factoryOperation)
    {
        // Example: Could log or prepare for the operation
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after each of Order's Task-returning operations completes successfully.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: the async complete hook runs after a Task-returning
    /// operation succeeds, before the result is returned to the caller. Like the
    /// start hook, it does not run for synchronous, constructor or static
    /// operations, nor for a class-level [Execute], nor after a bool operation
    /// (Fetch, FetchDraft) that returns false.
    ///
    /// Use cases:
    /// - Resetting tracking state
    /// - Logging completion
    ///
    /// When an operation throws, the start hook has already run and the complete
    /// hook does not. When an async instance operation is cancelled, IFactoryOnCancelled / IFactoryOnCancelledAsync runs instead,
    /// then the cancellation propagates.
    /// </remarks>
    #region operations-lifecycle-oncomplete
    public Task FactoryCompleteAsync(FactoryOperation factoryOperation)
    {
        // Example: Reset IsNew after successful Insert
        if (factoryOperation == FactoryOperation.Insert)
        {
            IsNew = false;
        }
        return Task.CompletedTask;
    }
    #endregion

    // -------------------------------------------------------------------------
    // Factory Operations
    // -------------------------------------------------------------------------

    // The operations README and getting started embed (region readme-domain-model).
    // Why each is shaped this way, kept here so the embedded code stays short:
    //
    // DESIGN DECISION: A root's Create is local (VisionAlignment D14)
    //   Create is the equivalent of `new`. Building an empty draft order needs
    //   nothing only the server has, so the client creates it without a round
    //   trip; the order first reaches the server when it is saved. A root's Create
    //   is [Remote] only when creation needs the server: tenant defaults, a
    //   sequence, or an [AuthorizeFactory] check that needs server services (the
    //   check runs inside Create, on whichever tier runs it; the Save is enforced
    //   on the server either way). It is public because it is not [Remote]: an
    //   internal Create would be server-only and guarded. The [Service]
    //   IOrderLineListFactory resolves on the calling tier; every generated
    //   factory is registered on both tiers.
    //
    // DESIGN DECISION: A missing order is a false Fetch, not an exception
    //   Fetch is the one operation that handles "not there" cleanly: it returns
    //   false and the factory returns null (VisionAlignment R-I). The load is
    //   symmetric with the save: the list receives the row's line rows and hands
    //   each line its own row (R-D).
    //
    // GENERATOR BEHAVIOR: Save routes on IsNew and IsDeleted
    //   IsNew=true -> Insert; IsNew=false, IsDeleted=false -> Update;
    //   IsNew=false, IsDeleted=true -> Delete.
    //
    // THE CASCADE (R-D): Insert and Update make or find the order's row, map the
    //   order into it, hand the row's line collection to the list factory's Save,
    //   and flush once. The list and each line do the rest; the root never maps a
    //   line.
    //
    // DESIGN DECISION: A missing row in Update or Delete is an exception
    //   The order was loaded, so its row existing is the expected state; a row
    //   deleted underneath it is a failure the caller sees (VisionAlignment R-I).
    #region readme-domain-model
    #region operations-create-method
    /// <summary>
    /// Creates a new Order. Local: runs on the tier that calls it.
    /// </summary>
    [Create]
    public void Create(
        string customerName,
        [Service] IOrderLineListFactory lineListFactory)
    {
        CustomerName = customerName;
        Lines = lineListFactory.Create();
        Status = OrderStatus.Draft;
        IsNew = true;
    }
    #endregion

    #region operations-fetch-instance
    /// <summary>
    /// Loads an existing Order and its lines; false when there is none.
    /// </summary>
    [Remote, Fetch]
    internal async Task<bool> Fetch(
        int id,
        [Service] IOrderDbContext db,
        [Service] IOrderLineListFactory lineListFactory)
    {
        var row = await db.FindAsync(id);
        if (row == null)
            return false;

        LoadFrom(row, lineListFactory);
        return true;
    }
    #endregion

    #region operations-insert
    /// <summary>
    /// Inserts a new Order and its lines.
    /// </summary>
    [Remote, Insert]
    internal async Task Insert(
        [Service] IOrderDbContext db,
        [Service] IOrderLineListFactory lineListFactory)
    {
        var row = new OrderRow();
        MapTo(row);
        db.Add(row);
        lineListFactory.Save(Lines, row.Lines);
        await db.SaveChangesAsync();
        Id = row.Id;
    }
    #endregion

    #region operations-update
    /// <summary>
    /// Updates an existing Order and its lines.
    /// </summary>
    [Remote, Update]
    internal async Task Update(
        [Service] IOrderDbContext db,
        [Service] IOrderLineListFactory lineListFactory)
    {
        var row = await db.FindAsync(Id)
            ?? throw new KeyNotFoundException($"Order {Id} not found");
        MapTo(row);
        lineListFactory.Save(Lines, row.Lines);
        await db.SaveChangesAsync();
    }
    #endregion

    #region operations-delete
    /// <summary>
    /// Deletes an Order. Its line rows go with its row.
    /// </summary>
    [Remote, Delete]
    internal async Task Delete([Service] IOrderDbContext db)
    {
        var row = await db.FindAsync(Id)
            ?? throw new KeyNotFoundException($"Order {Id} not found");
        db.Remove(row);
        await db.SaveChangesAsync();
    }
    #endregion
    #endregion

    /// <summary>
    /// Loads the customer's Draft order. Server-only: <see cref="OpenDraft"/> calls it.
    /// </summary>
    /// <remarks>
    /// Internal without [Remote], so the client cannot call it; the client asks for
    /// a draft through OpenDraft, which decides between this and Create.
    /// </remarks>
    #region skill-order-fetchdraft
    [Fetch]
    internal async Task<bool> FetchDraft(
        string customerName,
        [Service] IOrderDbContext db,
        [Service] IOrderLineListFactory lineListFactory)
    {
        var row = await db.FindDraftAsync(customerName);
        if (row == null)
            return false;

        LoadFrom(row, lineListFactory);
        return true;
    }
    #endregion

    /// <summary>
    /// The customer's Draft order: the stored one if there is one, otherwise a new one.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Create-or-fetch is an [Execute] that calls Fetch, then Create (VisionAlignment D11)
    ///
    /// One round trip. On the server it fetches the customer's Draft and, when there
    /// is none, creates one through the same factory. A new order is returned unsaved:
    /// it reaches the store when the client saves it.
    ///
    /// DID NOT DO THIS: A [Create] that looks for an existing order, or a [Fetch]
    /// that makes one when none is found
    ///
    /// Create is `new` and Fetch takes a key (D3). Either hybrid hides from the
    /// caller which one it got, and puts the decision in an operation whose name
    /// says the opposite.
    ///
    /// GENERATOR BEHAVIOR: a class-level [Execute] must return the class's service
    /// type, here Task&lt;IOrder&gt;. It is public static, and [Remote] sends the
    /// call to the server.
    /// </remarks>
    #region operations-class-execute
    [Remote, Execute]
    public static async Task<IOrder> OpenDraft(
        string customerName,
        [Service] IOrderFactory factory)
    {
        return await factory.FetchDraft(customerName)
            ?? factory.Create(customerName);
    }
    #endregion

    private void LoadFrom(OrderRow row, IOrderLineListFactory lineListFactory)
    {
        Id = row.Id;
        CustomerName = row.CustomerName;
        Status = row.Status;
        Lines = lineListFactory.Fetch(row.Lines);
        IsNew = false;
    }

    private void MapTo(OrderRow row)
    {
        row.CustomerName = CustomerName;
        row.Status = Status;
    }

    // -------------------------------------------------------------------------
    // Domain Methods
    //
    // DESIGN DECISION: Business logic lives in the entity, not the factory
    //
    // The factory handles Create/Fetch/Save lifecycle. Business operations
    // (AddLine, Submit, Cancel) are domain methods on the entity itself.
    //
    // DID NOT DO THIS: Put business logic in factory methods
    //
    // Reasons:
    // 1. Entity encapsulates its behavior - DDD principle
    // 2. Factory is infrastructure, not domain
    // 3. Easier to test domain logic in isolation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Adds a line the caller built with IOrderLineFactory.Create (R-C).
    /// </summary>
    public void AddLine(IOrderLine line)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Cannot add lines to a submitted order");

        Lines.AddLine(line);
    }

    public void RemoveLine(IOrderLine line)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Cannot remove lines from a submitted order");

        Lines.RemoveLine(line);
    }

    /// <summary>
    /// Whether the order can be submitted: a draft with at least one line.
    /// </summary>
    /// <remarks>
    /// The verdict a screen or a command reads before it calls Submit (R-E).
    /// Get-only, so it is not serialized; each tier computes it.
    /// </remarks>
    public bool CanSubmit => Status == OrderStatus.Draft && Lines.Count > 0;

    public void Submit()
    {
        // Calling Submit when CanSubmit is false is a caller bug, not a user error
        if (!CanSubmit)
            throw new InvalidOperationException(Status != OrderStatus.Draft
                ? "Order is not in draft status"
                : "Cannot submit an empty order");

        Status = OrderStatus.Submitted;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Order is already cancelled");

        Status = OrderStatus.Cancelled;
    }

    public void MarkDeleted()
    {
        IsDeleted = true;
    }
}

/// <summary>
/// Order status enumeration.
/// </summary>
public enum OrderStatus
{
    Draft,
    Submitted,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}
