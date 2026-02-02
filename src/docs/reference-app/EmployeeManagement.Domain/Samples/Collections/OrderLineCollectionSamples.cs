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

    [Create]
    public void Create(string name, decimal price, int qty)
    {
        Id = Random.Shared.Next(1, 10000);
        Name = name;
        Price = price;
        Quantity = qty;
    }

    [Fetch]
    public void Fetch(int id, string name, decimal price, int qty)
    {
        Id = id;
        Name = name;
        Price = price;
        Quantity = qty;
    }
}

/// <summary>
/// Collection of OrderLines within an Order aggregate.
/// </summary>
[Factory]
public partial class OrderLineList : List<OrderLine>
{
    private readonly IOrderLineFactory _lineFactory;

    #region collection-factory-basic
    // Constructor-injected child factory (survives serialization)
    [Create]
    public OrderLineList([Service] IOrderLineFactory lineFactory) => _lineFactory = lineFactory;

    // Fetch populates collection from data
    [Fetch]
    public void Fetch(
        IEnumerable<(int id, string name, decimal price, int qty)> items,
        [Service] IOrderLineFactory lineFactory)
    {
        foreach (var item in items)
            Add(lineFactory.Fetch(item.id, item.name, item.price, item.qty));
    }

    // Domain method uses stored factory to add children
    public void AddLine(string name, decimal price, int qty) => Add(_lineFactory.Create(name, price, qty));
    #endregion
}

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

    #region collection-factory-parent
    // Parent creates collection via factory - collection is properly initialized with child factory
    [Remote, Create]
    public void Create(string customerName, [Service] IOrderLineListFactory lineListFactory)
    {
        Id = Random.Shared.Next(1, 10000);
        CustomerName = customerName;
        Lines = lineListFactory.Create();
    }

    [Remote, Fetch]
    public void Fetch(int id, [Service] IOrderLineListFactory lineListFactory)
    {
        Id = id;
        Lines = lineListFactory.Fetch([(1, "Widget A", 10.00m, 2), (2, "Widget B", 25.00m, 1)]);
    }
    #endregion
}
