// =============================================================================
// DESIGN SOURCE OF TRUTH: A class hierarchy, each concrete type with its factory
// =============================================================================
//
// An order is paid by card or on invoice. Both payments share an order and an
// amount; each has data of its own. Payment is the abstract base, CardPayment
// and InvoicePayment are the concrete types, and IPaymentLookup is the domain
// service that decides once which one an order has.
//
// GENERATOR BEHAVIOR: How [Factory] and the operations follow inheritance
//
// - A class gets a factory from its OWN [Factory] only. Payment has none, and
//   [Factory] on an abstract or generic class would be ignored anyway; each
//   concrete payment carries its own. There is no IPaymentFactory.
// - A derived [Factory] class's factory includes its base classes' operation
//   methods, with their [Remote], [Service] and [AspAuthorize]. Payment's
//   [Create] is on ICardPaymentFactory and IInvoicePaymentFactory. Constructors
//   are the exception: only the class's own count.
// - Each concrete payment crosses the wire as its own type, base properties
//   included, also when the declared type is IPayment.
//
// DID NOT DO THIS: A generic base with a self-referencing constraint
//
// A Neatoo base class needs `where T : Base<T>` for Neatoo's own generated
// property code. RemoteFactory needs nothing of the kind: a plain abstract base
// is enough.
//
// =============================================================================

using Neatoo.RemoteFactory;

namespace Design.Domain.Payments;

#region skill-hierarchy-base
/// <summary>
/// What every payment answers, whatever its kind.
/// </summary>
public interface IPayment
{
    int OrderId { get; }
    decimal Amount { get; }
}

/// <summary>
/// The shared shape and the shared operation. No [Factory]: only the concrete
/// payments have factories, and this Create joins each of them.
/// </summary>
public abstract class Payment : IPayment
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }

    /// <summary>
    /// A new payment for an order. Local: the client calls it on either concrete factory.
    /// </summary>
    [Create]
    public void Create(int orderId, decimal amount)
    {
        OrderId = orderId;
        Amount = amount;
    }
}
#endregion

#region skill-hierarchy-leaves
public interface ICardPayment : IPayment
{
    string CardLast4 { get; set; }
}

/// <summary>
/// A payment by card. Its own [Factory], its own data, its own load.
/// </summary>
[Factory]
internal partial class CardPayment : Payment, ICardPayment
{
    public string CardLast4 { get; set; } = string.Empty;

    [Remote, Fetch]
    internal bool Fetch(int orderId, [Service] IPaymentLedger ledger)
    {
        var row = ledger.Find(orderId);
        if (row is not { Kind: PaymentKind.Card })
            return false;

        OrderId = row.OrderId;
        Amount = row.Amount;
        CardLast4 = row.CardLast4 ?? string.Empty;
        return true;
    }
}

public interface IInvoicePayment : IPayment
{
    int NetDays { get; set; }
}

/// <summary>
/// A payment on invoice, due a number of days after the order.
/// </summary>
[Factory]
internal partial class InvoicePayment : Payment, IInvoicePayment
{
    public int NetDays { get; set; }

    [Remote, Fetch]
    internal bool Fetch(int orderId, [Service] IPaymentLedger ledger)
    {
        var row = ledger.Find(orderId);
        if (row is not { Kind: PaymentKind.Invoice })
            return false;

        OrderId = row.OrderId;
        Amount = row.Amount;
        NetDays = row.NetDays;
        return true;
    }
}
#endregion

#region skill-hierarchy-lookup
/// <summary>
/// The payment an order has, whichever kind it is. Executed on the server, called from the client.
/// </summary>
[Factory]
public interface IPaymentLookup
{
    Task<IPayment?> ForOrderAsync(int orderId);
}

/// <summary>
/// Decides once, at the boundary, which concrete payment to load. Code that
/// receives the payment works with its type, not with a kind flag.
/// </summary>
public class PaymentLookup(
    IPaymentLedger ledger,
    ICardPaymentFactory cardPayments,
    IInvoicePaymentFactory invoicePayments) : IPaymentLookup
{
    public async Task<IPayment?> ForOrderAsync(int orderId)
    {
        // On the server already: the [Remote] Fetch calls below run here, locally
        return ledger.Find(orderId)?.Kind switch
        {
            PaymentKind.Card => await cardPayments.Fetch(orderId),
            PaymentKind.Invoice => await invoicePayments.Fetch(orderId),
            _ => null,
        };
    }
}
#endregion

// -----------------------------------------------------------------------------
// The server's payment store: an in-memory stand-in for a database
// -----------------------------------------------------------------------------

public enum PaymentKind
{
    Card,
    Invoice,
}

/// <summary>
/// One order's payment as the store holds it: one row for both kinds.
/// </summary>
public record PaymentRow(int OrderId, PaymentKind Kind, decimal Amount, string? CardLast4, int NetDays);

/// <summary>
/// The server's payment store. Server-only: reached by [Service] parameters and
/// by PaymentLookup's constructor on the server.
/// </summary>
public interface IPaymentLedger
{
    PaymentRow? Find(int orderId);
}

/// <summary>
/// Registered by RegisterMatchingName (IPaymentLedger -> PaymentLedger).
/// </summary>
public class PaymentLedger : IPaymentLedger
{
    public const int CardOrderId = 100;
    public const int InvoiceOrderId = 200;

    private static readonly PaymentRow[] Rows =
    [
        new(CardOrderId, PaymentKind.Card, 37.50m, "4242", 0),
        new(InvoiceOrderId, PaymentKind.Invoice, 120.00m, null, 30),
    ];

    public PaymentRow? Find(int orderId) => Rows.FirstOrDefault(r => r.OrderId == orderId);
}
