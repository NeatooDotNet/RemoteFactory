using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Neatoo.RemoteFactory;
#if !CLIENT
using OrderEntry.Ef;
#endif

namespace OrderEntry.Domain;

/// <summary>
/// Order line collection.
/// Child collection that manages order line items.
/// </summary>
[Factory]
internal class OrderLineList : IOrderLineList
{
    private readonly List<IOrderLine> _lines = new();

    public int Count => _lines.Count;

    public IOrderLine this[int index] => _lines[index];

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Add(IOrderLine line)
    {
        _lines.Add(line);
        OnPropertyChanged(nameof(Count));
    }

    public void Remove(IOrderLine line)
    {
        _lines.Remove(line);
        OnPropertyChanged(nameof(Count));
    }

    public void Clear()
    {
        _lines.Clear();
        OnPropertyChanged(nameof(Count));
    }

    public IEnumerator<IOrderLine> GetEnumerator() => _lines.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Local Create - runs on both client and server.
    /// Creates an empty line collection.
    /// </summary>
    [Create]
    public OrderLineList()
    {
    }

#if !CLIENT
    /// <summary>
    /// Server-only Fetch - loads lines from EF entities.
    /// Called by parent Order.Fetch to populate children.
    /// </summary>
    [Fetch]
    public void Fetch(
        ICollection<OrderLineEntity> entities,
        [Service] IOrderLineFactory lineFactory)
    {
        _lines.Clear();
        foreach (var entity in entities)
        {
            var line = lineFactory.Fetch(entity);
            _lines.Add(line);
        }
    }
#endif
}
