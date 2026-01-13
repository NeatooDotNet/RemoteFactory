using System.Collections;
using System.ComponentModel;

namespace OrderEntry.Domain;

/// <summary>
/// Order line collection interface.
/// Extends IEnumerable for data binding and iteration.
/// </summary>
public interface IOrderLineList : IEnumerable<IOrderLine>, INotifyPropertyChanged
{
    int Count { get; }
    IOrderLine this[int index] { get; }
    void Add(IOrderLine line);
    void Remove(IOrderLine line);
    void Clear();
}
