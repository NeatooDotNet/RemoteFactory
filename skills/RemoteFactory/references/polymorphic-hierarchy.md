# Class Hierarchies

Several concrete types can share a base class, each with its own factory. The example is RemoteFactory Design's payments: an order is paid by card or on invoice, both share an order and an amount, and each has data of its own.

## The base

<!-- snippet: skill-hierarchy-base -->
<a id='snippet-skill-hierarchy-base'></a>
```cs
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
```
<sup><a href='/src/Design/Design.Domain/Payments/Payment.cs#L34-L63' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-hierarchy-base' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## The concrete types

<!-- snippet: skill-hierarchy-leaves -->
<a id='snippet-skill-hierarchy-leaves'></a>
```cs
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
```
<sup><a href='/src/Design/Design.Domain/Payments/Payment.cs#L65-L119' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-hierarchy-leaves' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## How `[Factory]` and the operations follow inheritance

- **`[Factory]` is per class.** A class gets a factory from its own `[Factory]` only. A derived class without one has no factory, whatever its base carries, although `FactoryAttribute` declares `Inherited = true` ([#141](https://github.com/NeatooDotNet/RemoteFactory/issues/141)). `[Factory]` on an `abstract` or generic class is ignored. So `Payment` has no factory and there is no `IPaymentFactory`; each concrete payment carries its own `[Factory]`.
- **Operation methods are inherited.** A derived `[Factory]` class's factory includes its base classes' operation methods - `[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`, `[Execute]` - with their `[Remote]`, `[Service]` and `[AspAuthorize]`. `Payment.Create` is on both `ICardPaymentFactory` and `IInvoicePaymentFactory`.
- **Constructors are not.** Only the derived class's own `[Create]` constructors count.
- **Authorization is inherited.** A derived class without its own `[AuthorizeFactory<T>]` takes the nearest base class's; its own replaces the base's.
- **Each concrete type has its own factory interface**, returning its own interface (`ICardPayment`).
- **No self-referencing generic constraint is needed.** A plain abstract base is enough. A Neatoo base class needs `where T : Base<T>` for Neatoo's own generated code; that is the Neatoo skill's rule, not RemoteFactory's.

## Deciding which type to load

A domain service the client calls reads the store once and loads the right concrete type. Code that receives the payment works with its type, not with a kind flag:

<!-- snippet: skill-hierarchy-lookup -->
<a id='snippet-skill-hierarchy-lookup'></a>
```cs
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
```
<sup><a href='/src/Design/Design.Domain/Payments/Payment.cs#L121-L151' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-hierarchy-lookup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The service returns `IPayment`. Each concrete payment crosses the wire as its own type, base properties included, so the client receives the `ICardPayment` or `IInvoicePayment` the server chose. On the server, the service's calls to the concrete factories' `[Remote]` `Fetch` run locally, with no second round trip.
