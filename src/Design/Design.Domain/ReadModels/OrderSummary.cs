// =============================================================================
// DESIGN SOURCE OF TRUTH: Read Model
// =============================================================================
//
// A read model is a [Factory] class with [Fetch] only: the client holds an
// instance of it and a screen binds to it, but nothing edits it, so it has no
// [Create] and no Save (VisionAlignment R-H).
//
// DESIGN DECISION: A read model, not a DTO returned from a controller
//
// The screen asks "what does order 123 look like?" The answer is an object
// the client holds, loaded by one [Remote, Fetch]. There is no controller and
// no hand-written transfer class: the read model IS the type both tiers share.
// Its line summaries are plain records carried as a property; a record needs
// no [Factory] to cross the wire (R-F).
//
// DESIGN DECISION: Missing order -> false -> null
//
// Fetch is the one operation that handles "not there" cleanly: it returns
// false and the generated factory returns null (R-I).
//
// DID NOT DO THIS: Give the read model a [Create]
//
// Nothing creates an order summary; it reflects what the store holds. A
// [Create] would claim the object can be new, and a reader would look for a
// Save that does not exist.
//
// =============================================================================

using Design.Domain.Aggregates;
using Neatoo.RemoteFactory;

namespace Design.Domain.ReadModels;

/// <summary>
/// Public interface for the order summary read model.
/// </summary>
public interface IOrderSummary
{
    int OrderId { get; }
    string CustomerName { get; }
    OrderStatus Status { get; }
    decimal Total { get; }
    IReadOnlyList<OrderSummaryLine> Lines { get; }
}

/// <summary>
/// One line of an order summary: a plain record, no [Factory].
/// </summary>
public record OrderSummaryLine(string ProductName, int Quantity, decimal LineTotal);

/// <summary>
/// Read-only view of an order for a screen that displays it.
/// </summary>
/// <remarks>
/// GENERATOR BEHAVIOR: A [Fetch]-only class gets a factory with Fetch and no
/// Save. Because Fetch returns bool, IOrderSummaryFactory.Fetch returns
/// Task&lt;IOrderSummary?&gt;.
///
/// Properties have public setters: ordinal serialization writes only
/// properties it can set on the other side.
/// </remarks>
[Factory]
internal partial class OrderSummary : IOrderSummary
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public IReadOnlyList<OrderSummaryLine> Lines { get; set; } = [];

    /// <summary>
    /// Loads the summary on the server from the order's row.
    /// </summary>
    /// <remarks>
    /// A read model may read the store directly: it writes nothing, so no
    /// aggregate's rules are bypassed. It reads the same IOrderDbContext the
    /// Order aggregate writes.
    ///
    /// The CancellationToken is not serialized and not a [Service]. Over HTTP it is
    /// the request's: it fires when the client cancels or disconnects (a screen
    /// navigating away while the summary loads), or when the server shuts down.
    /// Passing it to the store is what lets the store stop work nobody will read.
    /// GENERATOR BEHAVIOR: the factory's Fetch takes an optional CancellationToken
    /// either way; declaring one here is what hands it to this method.
    /// </remarks>
    #region aspnetcore-cancellation
    [Remote, Fetch]
    internal async Task<bool> Fetch(
        int orderId,
        [Service] IOrderDbContext db,
        CancellationToken cancellationToken)
    {
        var row = await db.FindAsync(orderId, cancellationToken);
        if (row == null)
            return false;

        OrderId = row.Id;
        CustomerName = row.CustomerName;
        Status = row.Status;
        Lines = row.Lines
            .Select(l => new OrderSummaryLine(l.ProductName, l.Quantity, l.UnitPrice * l.Quantity))
            .ToList();
        Total = Lines.Sum(l => l.LineTotal);
        return true;
    }
    #endregion
}
