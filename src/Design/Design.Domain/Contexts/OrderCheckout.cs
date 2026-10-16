// =============================================================================
// DESIGN SOURCE OF TRUTH: Context
// =============================================================================
//
// A context is a plain [Factory] class a screen binds to when later verbs act
// on what it loaded (VisionAlignment D13). OrderCheckout holds an order: the
// checkout screen loads it once, the user adds products and submits, and the
// context saves the order through the order's own root factory.
//
// DESIGN DECISION: Verbs are plain instance methods
//
// AddProduct and Submit call the held order's verbs; SaveAsync calls the
// order's root Save. None of them is a factory operation. An instance
// [Execute] does not exist (NF0103), and a verb that persisted on its own
// would bypass the aggregate root.
//
// DESIGN DECISION: The factories arrive through the constructor (T14)
//
// The verbs run on the client after the context crossed the wire, so the
// services they use must be there on the client. Constructor injection runs on
// both tiers: the context is built through DI on whichever side deserializes
// it, so IOrderFactory and IOrderLineFactory are that side's. A method-
// injected service stored in a field would be null after the crossing.
//
// GENERATOR BEHAVIOR: A constructor that needs services means the class is not
// ordinal-serialized; it crosses in named form and is created through DI on
// each side (as CtorInjectedEntity in CtorInjectionExample.cs).
//
// DID NOT DO THIS: Look the product up by SKU inside AddProduct
//
// The screen already holds the product it showed the user (from
// IProductCatalog). A lookup inside the verb would be a server call per click
// that the user did not ask for.
//
// CONTRAST WITH: a static command (ExampleCommands)
//
// A command is for a call that returns and is done. A context is for a screen
// that binds to the result and acts on it afterwards (D12, D13).
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Domain.Entities;
using Design.Domain.FactoryPatterns;
using Neatoo.RemoteFactory;

namespace Design.Domain.Contexts;

/// <summary>
/// Public interface for the checkout context.
/// </summary>
public interface IOrderCheckout
{
    IOrder Order { get; }
    void AddProduct(ProductListing product, int quantity);
    void Submit();
    Task SaveAsync();
}

/// <summary>
/// Checkout context: the order a checkout screen is working on.
/// </summary>
#region skill-context-checkout
[Factory]
internal partial class OrderCheckout : IOrderCheckout
{
    private readonly IOrderFactory _orderFactory;
    private readonly IOrderLineFactory _lineFactory;

    public OrderCheckout(IOrderFactory orderFactory, IOrderLineFactory lineFactory)
    {
        _orderFactory = orderFactory;
        _lineFactory = lineFactory;
    }

    public IOrder Order { get; set; } = null!;

    /// <summary>
    /// Loads the context and its order in one call.
    /// </summary>
    /// <remarks>
    /// Runs on the server: the order's own Fetch runs there too, as a local
    /// call, so the screen's whole state arrives in one round trip. A missing
    /// order is a false Fetch, and the factory returns null (R-I).
    /// </remarks>
    [Remote, Fetch]
    internal async Task<bool> Fetch(int orderId)
    {
        var order = await _orderFactory.Fetch(orderId);
        if (order == null)
            return false;

        Order = order;
        return true;
    }

    /// <summary>
    /// Adds the product the screen holds as a line on the order. No server call.
    /// </summary>
    public void AddProduct(ProductListing product, int quantity)
    {
        Order.AddLine(_lineFactory.Create(product.Name, product.UnitPrice, quantity));
    }

    /// <summary>
    /// Submits the order. No server call; the change is saved by SaveAsync.
    /// </summary>
    public void Submit()
    {
        Order.Submit();
    }

    /// <summary>
    /// Saves the order through its root factory and holds the saved instance.
    /// </summary>
    /// <remarks>
    /// Save returns a new instance from the server; the context reassigns it.
    /// </remarks>
    public async Task SaveAsync()
    {
        Order = await _orderFactory.Save(Order)
            ?? throw new InvalidOperationException("Saving the order returned nothing.");
    }
}
#endregion
