// =============================================================================
// DESIGN SOURCE OF TRUTH: Child Entity and Child Collection
// =============================================================================
//
// OrderLine is a child entity of Order; OrderLineList is the collection Order
// holds. Both are `internal partial` classes behind public interfaces.
//
// VISIBILITY (VisionAlignment R-C):
//
//   [Create]   public, no [Remote]  -> the client builds a line, then adds it
//   [Fetch]    internal             -> server-only, guarded, trimmed on the client
//   [Insert]   internal             -> server-only, reached through Save
//   [Update]   internal             -> server-only, reached through Save
//
// Because Create is public, the generated IOrderLineFactory is a PUBLIC
// interface whose Fetch and Save are `internal` interface members. The client
// sees Create and nothing else; the server-side cascade calls the rest.
//
// [Remote] goes on no child operation. A child's operations run after the
// root's [Remote] call has already crossed, so [Remote] there marks a crossing
// that never happens: a placement and trimming error (VisionAlignment D18).
//
// THE CASCADE (VisionAlignment R-D): see OrderPersistence.cs. The list hands
// each line its own row; the line maps itself into it.
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Domain.ValueObjects;
using Neatoo.RemoteFactory;

namespace Design.Domain.Entities;

/// <summary>
/// Public interface for OrderLine child entity.
/// </summary>
/// <remarks>
/// Extends IFactorySaveMeta so the generated IOrderLineFactory.Save can route
/// on IsNew/IsDeleted through the interface.
/// </remarks>
public interface IOrderLine : IFactorySaveMeta
{
    Guid Id { get; }
    string ProductName { get; }
    Money UnitPrice { get; }
    int Quantity { get; }
    Money LineTotal { get; }
    void UpdateQuantity(int newQuantity);
    void UpdatePrice(decimal newUnitPrice);
}

/// <summary>
/// Public interface for the OrderLine collection.
/// </summary>
/// <remarks>
/// Enumerates live lines only: a removed line is kept, flagged IsDeleted, until
/// the next save deletes its row, and it is hidden here and from the total.
/// </remarks>
public interface IOrderLineList : IReadOnlyList<IOrderLine>, IFactorySaveMeta
{
    void AddLine(IOrderLine line);
    void RemoveLine(IOrderLine line);
    Money CalculateTotal();
}

/// <summary>
/// Child entity representing a line item in an Order.
/// </summary>
/// <remarks>
/// DESIGN DECISION: The line's identity is generated when it is created
///
/// A Guid assigned in Create lets the list find an existing line's row on
/// every later save, with no wait for the store to assign an id.
///
/// DESIGN DECISION: The line maps itself into the row it is handed
///
/// The list makes or finds the row and passes it through the line factory's
/// Save; Insert and Update map this line into it. The parent never maps a
/// child's row.
/// </remarks>
[Factory]
internal partial class OrderLine : IOrderLine
{
    // -------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------

    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public Money UnitPrice { get; set; } = Money.Zero;
    public int Quantity { get; set; }
    public Money LineTotal => UnitPrice.Multiply(Quantity);

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    // -------------------------------------------------------------------------
    // Factory Operations
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a new OrderLine. Public and local: the client calls it.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: A child's Create is the one public child operation (R-C)
    ///
    /// The client builds the line with IOrderLineFactory.Create(...), then hands
    /// it to Order.AddLine. No server call: creating a line needs nothing only
    /// the server has.
    ///
    /// DID NOT DO THIS: Store the line factory on the list and create inside AddLine
    ///
    /// Reasons:
    /// 1. A method-injected service stored in a field does not survive
    ///    serialization; after a remote fetch the field is null on the client
    /// 2. The caller passing what Create needs is the plainest shape
    /// </remarks>
    [Create]
    public void Create(string productName, decimal unitPrice, int quantity)
    {
        Id = Guid.NewGuid();
        ProductName = productName;
        UnitPrice = new Money(unitPrice);
        Quantity = quantity;
        IsNew = true;
    }

    /// <summary>
    /// Loads an existing OrderLine from its row. Called by OrderLineList.Fetch.
    /// </summary>
    /// <remarks>
    /// DID NOT DO THIS: Make OrderLine.Fetch [Remote]
    ///
    /// Order.Fetch has already crossed to the server and is loading this line;
    /// there is no client call here for [Remote] to mark.
    /// </remarks>
    [Fetch]
    internal void Fetch(OrderLineRow row)
    {
        Id = row.Id;
        ProductName = row.ProductName;
        UnitPrice = new Money(row.UnitPrice);
        Quantity = row.Quantity;
        IsNew = false;
    }

    /// <summary>
    /// Maps a new line into the row the list made for it.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: The line factory's Save routes here when IsNew is true.
    /// The line is not new once inserted, so the aggregate the save returns can
    /// be saved again.
    /// </remarks>
    [Insert]
    internal void Insert(OrderLineRow row)
    {
        row.Id = Id;
        MapTo(row);
        IsNew = false;
    }

    /// <summary>
    /// Maps an existing line into the row the list found for it.
    /// </summary>
    [Update]
    internal void Update(OrderLineRow row)
    {
        MapTo(row);
    }

    private void MapTo(OrderLineRow row)
    {
        row.ProductName = ProductName;
        row.UnitPrice = UnitPrice.Amount;
        row.Quantity = Quantity;
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
/// DESIGN DECISION: A [Factory] list deriving from List&lt;T&gt;
///
/// On the wire the list is an array of its items in both serialization formats,
/// each item in that format's shape (v1.10.3, #119). Only the items cross: the
/// list's own properties do not. State the server needs about a removed line
/// therefore lives on the line (IsDeleted), not on the list.
///
/// DESIGN DECISION: The list is never new or deleted
///
/// Its IsNew and IsDeleted are always false, so the generated Save always routes
/// to Update, which runs the cascade. List-level flags would not cross the wire
/// anyway, and a Save branch with no operation throws NotImplementedException.
/// They are read-only for that reason: public settable IsNew/IsDeleted (R-M) are
/// for objects whose own state crosses the wire, which a collection's does not.
/// </remarks>
[Factory]
internal partial class OrderLineList : List<OrderLine>, IOrderLineList
{
    public bool IsNew => false;
    public bool IsDeleted => false;

    // -------------------------------------------------------------------------
    // IOrderLineList: live lines only
    //
    // The concrete List<OrderLine> holds removed lines too, flagged IsDeleted,
    // until the next save; that is what carries them to the server. The public
    // interface shows live lines only.
    // -------------------------------------------------------------------------

    private IEnumerable<OrderLine> Live => ((IEnumerable<OrderLine>)this).Where(l => !l.IsDeleted);

    IOrderLine IReadOnlyList<IOrderLine>.this[int index] => Live.ElementAt(index);

    int IReadOnlyCollection<IOrderLine>.Count => Live.Count();

    IEnumerator<IOrderLine> IEnumerable<IOrderLine>.GetEnumerator() => Live.GetEnumerator();

    // -------------------------------------------------------------------------
    // Factory Operations
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates an empty list. Public and local, so a root created on the client
    /// can create its list there (R-C).
    /// </summary>
    [Create]
    public void Create()
    {
    }

    /// <summary>
    /// Loads the lines from the order row's line rows, handing each line its row.
    /// </summary>
    [Fetch]
    internal void Fetch(IEnumerable<OrderLineRow> rows, [Service] IOrderLineFactory lineFactory)
    {
        foreach (var row in rows)
        {
            Add((OrderLine)lineFactory.Fetch(row));
        }
    }

    /// <summary>
    /// The cascade: makes, finds or removes each line's row and hands each live
    /// line its row through the line factory's Save.
    /// </summary>
    /// <remarks>
    /// Called by Order.Insert and Order.Update through IOrderLineListFactory.Save,
    /// with the order row's line collection. Neither the list nor the root maps a
    /// line's row; the line does.
    /// </remarks>
    [Update]
    internal void Update(ICollection<OrderLineRow> rows, [Service] IOrderLineFactory lineFactory)
    {
        foreach (var line in this)
        {
            if (line.IsDeleted)
            {
                rows.Remove(rows.Single(r => r.Id == line.Id));
                continue;
            }

            OrderLineRow row;
            if (line.IsNew)
            {
                row = new OrderLineRow();
                rows.Add(row);
            }
            else
            {
                row = rows.Single(r => r.Id == line.Id);
            }

            lineFactory.Save(line, row);
        }

        // The removed lines' rows are gone; the list the save returns holds live lines only.
        RemoveAll(l => l.IsDeleted);
    }

    // -------------------------------------------------------------------------
    // Domain Methods
    // -------------------------------------------------------------------------

    public void AddLine(IOrderLine line)
    {
        Add((OrderLine)line);
    }

    /// <summary>
    /// Removes a line. A line that was never saved is dropped; a saved line is
    /// flagged and kept until the next save deletes its row.
    /// </summary>
    public void RemoveLine(IOrderLine line)
    {
        var orderLine = (OrderLine)line;
        if (orderLine.IsNew)
        {
            Remove(orderLine);
        }
        else
        {
            orderLine.IsDeleted = true;
        }
    }

    public Money CalculateTotal()
    {
        return Live.Aggregate(Money.Zero, (total, line) => total.Add(line.LineTotal));
    }
}
