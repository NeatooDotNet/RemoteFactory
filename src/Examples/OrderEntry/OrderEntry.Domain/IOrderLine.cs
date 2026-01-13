using System.ComponentModel;
using Neatoo.RemoteFactory;

namespace OrderEntry.Domain;

/// <summary>
/// Order line item interface.
/// </summary>
public interface IOrderLine : INotifyPropertyChanged
{
    Guid Id { get; }
    string? ProductName { get; set; }
    int Quantity { get; set; }
    decimal UnitPrice { get; set; }
    decimal LineTotal { get; }
}
