using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Collections;

#region serialization-caveat-broken
/// <summary>
/// BROKEN PATTERN: Method-injected services stored in fields are lost after serialization.
/// </summary>
/// <remarks>
/// This demonstrates what NOT to do. After the object crosses the client-server
/// boundary, _lineFactory will be null because service references are not serialized.
/// </remarks>
[Factory]
public partial class OrderLineListBroken : List<OrderLineBroken>
{
    private IOrderLineBrokenFactory? _lineFactory;  // Lost after serialization!

    [Create]
    public OrderLineListBroken()
    {
    }

    [Fetch]
    public void Fetch(
        IEnumerable<(int id, string name, decimal price, int qty)> items,
        [Service] IOrderLineBrokenFactory lineFactory)  // Method injection
    {
        _lineFactory = lineFactory;  // Stored in field
        foreach (var item in items)
        {
            Add(lineFactory.Fetch(item.id, item.name, item.price, item.qty));
        }
    }

    public void AddLine(string name, decimal price, int qty)
    {
        // This fails after serialization - _lineFactory is null!
        var line = _lineFactory!.Create(name, price, qty);
        Add(line);
    }
}

/// <summary>
/// Child entity for the broken pattern sample.
/// </summary>
[Factory]
public partial class OrderLineBroken
{
    public int Id { get; private set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }

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
#endregion

#region serialization-caveat-fixed
/// <summary>
/// CORRECT PATTERN: Constructor-injected services survive serialization.
/// </summary>
/// <remarks>
/// With constructor injection, RemoteFactory resolves IOrderLineFixedFactory
/// from DI on both client and server. After deserialization, the factory
/// is resolved from the client's container.
/// </remarks>
[Factory]
public partial class OrderLineListFixed : List<OrderLineFixed>
{
    private readonly IOrderLineFixedFactory _lineFactory;  // Survives serialization!

    [Create]
    public OrderLineListFixed([Service] IOrderLineFixedFactory lineFactory)
    {
        _lineFactory = lineFactory;  // Constructor injection
    }

    [Fetch]
    public void Fetch(
        IEnumerable<(int id, string name, decimal price, int qty)> items,
        [Service] IOrderLineFixedFactory lineFactory)
    {
        // Can use lineFactory here for populating
        foreach (var item in items)
        {
            Add(lineFactory.Fetch(item.id, item.name, item.price, item.qty));
        }
    }

    public void AddLine(string name, decimal price, int qty)
    {
        // Works! _lineFactory is resolved from DI on both client and server
        var line = _lineFactory.Create(name, price, qty);
        Add(line);
    }
}

/// <summary>
/// Child entity for the fixed pattern sample.
/// </summary>
[Factory]
public partial class OrderLineFixed
{
    public int Id { get; private set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }

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
#endregion
