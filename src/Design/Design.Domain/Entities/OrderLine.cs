// =============================================================================
// DESIGN SOURCE OF TRUTH: Child Entity (No [Remote])
// =============================================================================
//
// This file demonstrates the CLASS FACTORY pattern for a child entity.
// Child entities are created and managed within their aggregate root's
// operations - they don't need [Remote] because they never cross the
// client/server boundary independently.
//
// =============================================================================

using Design.Domain.ValueObjects;
using Neatoo.RemoteFactory;

namespace Design.Domain.Entities;

/// <summary>
/// Child entity representing a line item in an Order.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Child entities do NOT have [Remote] on factory methods
///
/// Once execution reaches the server via the aggregate root's [Remote] method,
/// all subsequent operations stay server-side. Child entity factories are
/// called from within aggregate operations.
///
/// COMMON MISTAKE: Adding [Remote] to child entity methods
///
/// WRONG:
/// [Factory]
/// public partial class OrderLine {
///     [Remote, Create]  // <-- WRONG: Child doesn't need [Remote]
///     public void Create(...) { }
/// }
///
/// RIGHT:
/// [Factory]
/// public partial class OrderLine {
///     [Create]  // <-- No [Remote] - called from server-side Order operations
///     public void Create(...) { }
/// }
///
/// Why this matters:
/// 1. No unnecessary network round-trips for each child
/// 2. Atomic aggregate operations (all children created in one call)
/// 3. Consistent with DDD - children are part of aggregate transaction
///
/// GENERATOR BEHAVIOR: Without [Remote], the generator still creates:
/// - Factory interface: IOrderLineFactory
/// - Local Create/Fetch methods
/// - Serialization support
///
/// But it does NOT create remote stubs or delegates. The factory methods
/// execute directly in-process.
/// </remarks>
[Factory]
public partial class OrderLine
{
    // -------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------

    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Money UnitPrice { get; set; } = Money.Zero;
    public int Quantity { get; set; }
    public Money LineTotal => UnitPrice.Multiply(Quantity);

    // -------------------------------------------------------------------------
    // Factory Operations - Notice NO [Remote]
    //
    // DESIGN DECISION: Child entities use local factory operations
    //
    // These operations are called from within the aggregate root's operations,
    // which are already executing on the server. No remote crossing needed.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a new OrderLine.
    /// </summary>
    /// <remarks>
    /// Called from Order.AddLine() or OrderLineList.AddLine() - both
    /// execute server-side within an Order operation.
    /// </remarks>
    [Create]
    public void Create(string productName, decimal unitPrice, int quantity)
    {
        ProductName = productName;
        UnitPrice = new Money(unitPrice);
        Quantity = quantity;
    }

    /// <summary>
    /// Fetches an existing OrderLine.
    /// </summary>
    /// <remarks>
    /// Called from OrderLineList.Fetch() when loading an Order from
    /// the database. The Order.Fetch() method is [Remote], but this
    /// method is called after crossing to the server.
    ///
    /// DID NOT DO THIS: Have OrderLine.Fetch be [Remote]
    ///
    /// Reasons:
    /// 1. Order.Fetch already crossed to server - we're there
    /// 2. Would cause N+1 remote calls for N order lines
    /// 3. Breaks atomic aggregate loading
    ///
    /// The rule: [Remote] is only for the aggregate root entry point.
    /// Everything else executes server-side within that call.
    /// </remarks>
    [Fetch]
    public void Fetch(int id, string productName, decimal unitPrice, int quantity)
    {
        Id = id;
        ProductName = productName;
        UnitPrice = new Money(unitPrice);
        Quantity = quantity;
    }

    // -------------------------------------------------------------------------
    // Domain Methods
    // -------------------------------------------------------------------------

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(newQuantity));
        Quantity = newQuantity;
    }

    public void UpdatePrice(decimal newUnitPrice)
    {
        if (newUnitPrice < 0)
            throw new ArgumentException("Price cannot be negative", nameof(newUnitPrice));
        UnitPrice = new Money(newUnitPrice);
    }
}

/// <summary>
/// Collection of OrderLines within an Order.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Collection types get their own [Factory]
///
/// This enables:
/// 1. Type-safe collection creation via factory
/// 2. Batch Fetch with proper child factory injection
/// 3. Consistent serialization with parent aggregate
///
/// The collection's factory methods are also NOT [Remote] - they're
/// called from within Order operations.
/// </remarks>
[Factory]
public partial class OrderLineList : List<OrderLine>
{
    private IOrderLineFactory? _lineFactory;

    /// <summary>
    /// Creates a list with injected factory for adding lines.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Inject child factory in Create when collection
    /// needs to create children later.
    ///
    /// The _lineFactory is stored so AddLine can create new OrderLine
    /// instances without needing the caller to provide the factory.
    ///
    /// COMMON MISTAKE: Having multiple [Create] methods with same caller signature
    ///
    /// WRONG:
    /// [Create]
    /// public void Create() { }
    /// [Create]
    /// public void Create([Service] IFactory f) { }  // <-- Same caller signature!
    ///
    /// From the caller's perspective, both are Create() - the [Service] param
    /// is injected, not passed. This causes duplicate definition errors.
    ///
    /// RIGHT: Have one Create method that includes all needed services.
    /// </remarks>
    [Create]
    public void Create([Service] IOrderLineFactory lineFactory)
    {
        _lineFactory = lineFactory;
    }

    /// <summary>
    /// Fetches a list of OrderLines from data.
    /// </summary>
    [Fetch]
    public void Fetch(
        IEnumerable<(int id, string productName, decimal unitPrice, int quantity)> items,
        [Service] IOrderLineFactory lineFactory)
    {
        _lineFactory = lineFactory;

        foreach (var item in items)
        {
            var line = lineFactory.Fetch(item.id, item.productName, item.unitPrice, item.quantity);
            Add(line);
        }
    }

    // -------------------------------------------------------------------------
    // Domain Methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Adds a new line to the order.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Collection can create children via stored factory
    ///
    /// The factory was injected during Create/Fetch, so domain code can
    /// add new children without worrying about factory resolution.
    ///
    /// DID NOT DO THIS: Require factory parameter on every AddLine call
    ///
    /// Reasons:
    /// 1. Pollutes domain API with infrastructure concerns
    /// 2. Callers shouldn't know about factories
    /// 3. Factory injection is a one-time setup concern
    /// </remarks>
    public void AddLine(string productName, decimal unitPrice, int quantity)
    {
        if (_lineFactory == null)
            throw new InvalidOperationException("OrderLineList was not properly initialized with a factory");

        var line = _lineFactory.Create(productName, unitPrice, quantity);
        Add(line);
    }

    public Money CalculateTotal()
    {
        return this.Aggregate(Money.Zero, (total, line) => total.Add(line.LineTotal));
    }

    public void RemoveLine(OrderLine line)
    {
        Remove(line);
    }
}
