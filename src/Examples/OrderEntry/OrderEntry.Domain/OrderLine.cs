using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Neatoo.RemoteFactory;
using OrderEntry.Ef;

namespace OrderEntry.Domain;

/// <summary>
/// Order line item entity.
/// Local [Create] runs on both client and server. Server-only [Fetch] loads from EF.
/// </summary>
[Factory]
internal class OrderLine : IOrderLine
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Product name is required")]
    public string? ProductName { get; set { field = value; OnPropertyChanged(); } }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set { field = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineTotal)); } } = 1;

    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be positive")]
    public decimal UnitPrice { get; set { field = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineTotal)); } }

    public decimal LineTotal => Quantity * UnitPrice;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Local Create - runs on both client and server.
    /// No [Remote] attribute because this doesn't need server-side services.
    /// </summary>
    [Create]
    public OrderLine()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Server-only Fetch - loads line from EF entity.
    /// Called by OrderLineList.Fetch to populate children.
    /// </summary>
    [Fetch]
    public void Fetch(OrderLineEntity entity)
    {
        Id = entity.Id;
        ProductName = entity.ProductName;
        Quantity = entity.Quantity;
        UnitPrice = entity.UnitPrice;
    }
}
