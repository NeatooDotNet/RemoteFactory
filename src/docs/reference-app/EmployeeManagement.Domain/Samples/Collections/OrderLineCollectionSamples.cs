using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Collections;

/// <summary>
/// Individual line item in an Order - demonstrates child entity pattern.
/// </summary>
[Factory]
public partial class OrderLine
{
    public int Id { get; private set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    public decimal LineTotal => Price * Quantity;

    /// <summary>
    /// Creates a new order line.
    /// </summary>
    [Create]
    public void Create(string name, decimal price, int qty)
    {
        Id = Random.Shared.Next(1, 10000);
        Name = name;
        Price = price;
        Quantity = qty;
    }

    /// <summary>
    /// Fetches an order line from existing data.
    /// </summary>
    [Fetch]
    public void Fetch(int id, string name, decimal price, int qty)
    {
        Id = id;
        Name = name;
        Price = price;
        Quantity = qty;
    }
}

#region collection-factory-basic
/// <summary>
/// Collection of OrderLines within an Order aggregate.
/// </summary>
[Factory]
public partial class OrderLineList : List<OrderLine>
{
    private readonly IOrderLineFactory _lineFactory;

    /// <summary>
    /// Creates an empty collection with injected child factory.
    /// </summary>
    [Create]
    public OrderLineList([Service] IOrderLineFactory lineFactory)
    {
        _lineFactory = lineFactory;
    }

    /// <summary>
    /// Fetches a collection from data.
    /// </summary>
    [Fetch]
    public void Fetch(
        IEnumerable<(int id, string name, decimal price, int qty)> items,
        [Service] IOrderLineFactory lineFactory)
    {
        foreach (var item in items)
        {
            Add(lineFactory.Fetch(item.id, item.name, item.price, item.qty));
        }
    }

    /// <summary>
    /// Domain method using stored factory to add children.
    /// </summary>
    public void AddLine(string name, decimal price, int qty)
    {
        var line = _lineFactory.Create(name, price, qty);
        Add(line);
    }
}
#endregion

#region collection-factory-parent
/// <summary>
/// Order aggregate root demonstrating collection factory usage.
/// </summary>
[Factory]
public partial class Order
{
    public int Id { get; private set; }
    public string CustomerName { get; set; } = "";
    public OrderLineList Lines { get; set; } = null!;

    public decimal OrderTotal => Lines?.Sum(l => l.LineTotal) ?? 0;

    [Remote, Create]
    public void Create(
        string customerName,
        [Service] IOrderLineListFactory lineListFactory)
    {
        Id = Random.Shared.Next(1, 10000);
        CustomerName = customerName;
        Lines = lineListFactory.Create();  // Factory creates collection
    }

    [Remote, Fetch]
    public void Fetch(
        int id,
        [Service] IOrderLineListFactory lineListFactory)
    {
        Id = id;
        // Factory fetches collection with data
        Lines = lineListFactory.Fetch([
            (1, "Widget A", 10.00m, 2),
            (2, "Widget B", 25.00m, 1)
        ]);
    }
}
#endregion
