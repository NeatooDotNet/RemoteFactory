using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Collections;

/// <summary>
/// BROKEN PATTERN: Method-injected services stored in fields are lost after serialization.
/// </summary>
[Factory]
public partial class OrderLineListBroken : List<OrderLineBroken>
{
    #region serialization-caveat-broken
    // BROKEN: Method-injected service stored in field is lost after serialization
    private IOrderLineBrokenFactory? _lineFactory;  // NULL after crossing wire!

    [Fetch]
    public void Fetch(IEnumerable<(int id, string name, decimal price, int qty)> items,
        [Service] IOrderLineBrokenFactory lineFactory)
    {
        _lineFactory = lineFactory;  // Stored - but NOT serialized
        foreach (var item in items)
            Add(lineFactory.Fetch(item.id, item.name, item.price, item.qty));
    }

    public void AddLine(string name, decimal price, int qty)
    {
        var line = _lineFactory!.Create(name, price, qty);  // NullReferenceException!
        Add(line);
    }
    #endregion

    [Create]
    public OrderLineListBroken() { }
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
        Name = name; Price = price; Quantity = qty;
    }

    [Fetch]
    public void Fetch(int id, string name, decimal price, int qty)
    {
        Id = id; Name = name; Price = price; Quantity = qty;
    }
}

/// <summary>
/// CORRECT PATTERN: Constructor-injected services survive serialization.
/// </summary>
[Factory]
public partial class OrderLineListFixed : List<OrderLineFixed>
{
    #region serialization-caveat-fixed
    // CORRECT: Constructor injection - resolved from DI on BOTH client and server
    private readonly IOrderLineFixedFactory _lineFactory;

    [Create]
    public OrderLineListFixed([Service] IOrderLineFixedFactory lineFactory)
    {
        _lineFactory = lineFactory;  // Resolved on both sides after deserialization
    }

    public void AddLine(string name, decimal price, int qty)
    {
        var line = _lineFactory.Create(name, price, qty);  // Works on client!
        Add(line);
    }
    #endregion

    [Fetch]
    public void Fetch(IEnumerable<(int id, string name, decimal price, int qty)> items,
        [Service] IOrderLineFixedFactory lineFactory)
    {
        foreach (var item in items)
            Add(lineFactory.Fetch(item.id, item.name, item.price, item.qty));
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
        Name = name; Price = price; Quantity = qty;
    }

    [Fetch]
    public void Fetch(int id, string name, decimal price, int qty)
    {
        Id = id; Name = name; Price = price; Quantity = qty;
    }
}
