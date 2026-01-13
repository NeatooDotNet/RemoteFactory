using System.ComponentModel;
using Neatoo.RemoteFactory;

namespace OrderEntry.Domain;

/// <summary>
/// Order aggregate root interface.
/// </summary>
public interface IOrder : INotifyPropertyChanged, IFactorySaveMeta
{
    Guid Id { get; }
    string? CustomerName { get; set; }
    DateTime OrderDate { get; }
    decimal Total { get; }
    IOrderLineList Lines { get; }
    new bool IsDeleted { get; set; }
}
