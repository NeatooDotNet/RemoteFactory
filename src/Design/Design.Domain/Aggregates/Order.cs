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
//       Task<IOrder> Create(string customerName, ...);
//       Task<IOrder> Fetch(int id, ...);
//       Task<IOrder?> Save(IOrder target, ...);
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
public interface IOrder : IFactorySaveMeta
{
    int Id { get; set; }
    string CustomerName { get; set; }
    OrderStatus Status { get; set; }
    IOrderLineList Lines { get; set; }
    Money Total { get; }
    void AddLine(string productName, decimal unitPrice, int quantity);
    void Submit();
    void Cancel();
    void MarkDeleted();
}

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
[Factory]
internal partial class Order : IOrder, IFactorySaveMeta, IFactoryOnStartAsync, IFactoryOnCompleteAsync
{
    // -------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------

    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public IOrderLineList Lines { get; set; } = null!;
    public Money Total => Lines?.CalculateTotal() ?? Money.Zero;

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

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

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
    /// Called before any factory operation begins.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: FactoryStartAsync is called at the beginning of
    /// Create, Fetch, Insert, Update, and Delete operations.
    ///
    /// Use cases:
    /// - Logging operation start
    /// - Validating preconditions
    /// - Starting transactions
    /// - Loading related data
    ///
    /// COMMON MISTAKE: Throwing exceptions to prevent operations
    ///
    /// While you CAN throw to cancel an operation, consider returning
    /// a result type or using validation instead. Exceptions should be
    /// for truly exceptional conditions.
    /// </remarks>
    public Task FactoryStartAsync(FactoryOperation factoryOperation)
    {
        // Example: Could log, validate, or prepare for the operation
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after any factory operation completes successfully.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: FactoryCompleteAsync is called after the operation
    /// method returns but before the result is returned to the caller.
    ///
    /// Use cases:
    /// - Clearing dirty flags
    /// - Resetting tracking state
    /// - Committing transactions
    /// - Raising domain events
    ///
    /// DID NOT DO THIS: Call FactoryComplete on failure
    ///
    /// Reasons:
    /// 1. Clear semantics - complete means success
    /// 2. Use IFactoryOnCancelled for failure handling
    /// 3. Prevents confusing state after errors
    /// </remarks>
    public Task FactoryCompleteAsync(FactoryOperation factoryOperation)
    {
        // Example: Reset IsNew after successful Insert
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
    /// Creates a new Order on the server.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Aggregate root Create methods are [Remote]
    ///
    /// The client calls IOrderFactory.Create(), which crosses to the server.
    /// All child entity creation (OrderLine) happens server-side.
    ///
    /// COMMON MISTAKE: Making child factories [Remote]
    ///
    /// OrderLine.Create() should NOT be [Remote] because it's called from
    /// within Order operations that are already on the server.
    /// </remarks>
    [Remote, Create]
    internal void Create(
        string customerName,
        [Service] IOrderLineListFactory lineListFactory)
    {
        CustomerName = customerName;
        Lines = lineListFactory.Create();
        Status = OrderStatus.Draft;
        IsNew = true;
    }

    /// <summary>
    /// Fetches an existing Order from the server.
    /// </summary>
    [Remote, Fetch]
    internal void Fetch(
        int id,
        [Service] IOrderLineListFactory lineListFactory)
    {
        // Simulate loading from database
        Id = id;
        CustomerName = $"Customer_{id}";
        Status = OrderStatus.Submitted;
        Lines = lineListFactory.Fetch(
        [
            (1, "Widget A", 10.00m, 2),
            (2, "Widget B", 25.00m, 1)
        ]);
        IsNew = false;
    }

    /// <summary>
    /// Inserts a new Order to the database.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: When Save() is called and IsNew=true, IsDeleted=false,
    /// the factory routes to this Insert method.
    /// </remarks>
    [Remote, Insert]
    internal Task Insert([Service] IOrderRepository repository)
    {
        // In real code: repository.InsertAsync(this)
        // Simulate ID assignment
        Id = Random.Shared.Next(1000, 9999);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates an existing Order in the database.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: When Save() is called and IsNew=false, IsDeleted=false,
    /// the factory routes to this Update method.
    /// </remarks>
    [Remote, Update]
    internal Task Update([Service] IOrderRepository repository)
    {
        // In real code: repository.UpdateAsync(this)
        return Task.CompletedTask;
    }

    /// <summary>
    /// Deletes an Order from the database.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: When Save() is called and IsNew=false, IsDeleted=true,
    /// the factory routes to this Delete method.
    /// </remarks>
    [Remote, Delete]
    internal Task Delete([Service] IOrderRepository repository)
    {
        // In real code: repository.DeleteAsync(this)
        return Task.CompletedTask;
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

    public void AddLine(string productName, decimal unitPrice, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Cannot add lines to a submitted order");

        Lines.AddLine(productName, unitPrice, quantity);
    }

    public void Submit()
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Order is not in draft status");

        if (Lines.Count == 0)
            throw new InvalidOperationException("Cannot submit an empty order");

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

/// <summary>
/// Repository interface for Order persistence.
/// </summary>
/// <remarks>
/// Note: This is NOT a [Factory] interface - it's a plain service interface
/// for server-side use. The Order's Insert/Update/Delete methods use it.
///
/// Uses IOrder (public interface) so the repository interface can be public
/// even though the Order concrete class is internal.
/// </remarks>
public interface IOrderRepository
{
    Task InsertAsync(IOrder order);
    Task UpdateAsync(IOrder order);
    Task DeleteAsync(IOrder order);
}

/// <summary>
/// Mock implementation for testing.
/// </summary>
public class InMemoryOrderRepository : IOrderRepository
{
    private readonly List<IOrder> _orders = [];

    public Task InsertAsync(IOrder order)
    {
        _orders.Add(order);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(IOrder order)
    {
        // In-memory - nothing to do
        return Task.CompletedTask;
    }

    public Task DeleteAsync(IOrder order)
    {
        _orders.Remove(order);
        return Task.CompletedTask;
    }
}
