namespace OrderEntry.Ef;

/// <summary>
/// Order line database entity.
/// </summary>
public class OrderLineEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
