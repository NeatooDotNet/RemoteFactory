namespace OrderEntry.Ef;

/// <summary>
/// Order database entity.
/// </summary>
public class OrderEntity
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = null!;
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    
    /// <summary>
    /// Navigation property for order lines.
    /// Using init for EF Core compatibility while satisfying CA2227.
    /// </summary>
    public virtual ICollection<OrderLineEntity> Lines { get; init; } = new List<OrderLineEntity>();
}
