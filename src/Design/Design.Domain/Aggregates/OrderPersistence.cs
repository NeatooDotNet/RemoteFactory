// =============================================================================
// DESIGN SOURCE OF TRUTH: The Order aggregate's persistence model
// =============================================================================
//
// Rows are what the store holds; Order and OrderLine are what the client holds.
// Each object maps itself into the row it is handed (VisionAlignment R-D):
//
//   Order.Insert/Update   makes or finds its OrderRow, maps itself into it,
//                         hands row.Lines to the line list factory's Save,
//                         and flushes once.
//   OrderLineList.Update  makes a row for each new line, finds the row of each
//                         existing line, removes the row of each removed line,
//                         and hands each live line its row.
//   OrderLine.Insert/     maps itself into the row it is handed.
//   Update
//
// IOrderDbContext is an application interface injected with [Service] into the
// server-side operations. It is never a [Factory] interface: the client does
// not call it (VisionAlignment D12).
//
// =============================================================================

namespace Design.Domain.Aggregates;

/// <summary>
/// The stored shape of an order. Owns its line rows.
/// </summary>
public class OrderRow
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public List<OrderLineRow> Lines { get; } = [];
}

/// <summary>
/// The stored shape of an order line.
/// </summary>
public class OrderLineRow
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
/// Unit of work over order rows: find, add, remove, and one flush.
/// </summary>
public interface IOrderDbContext
{
    Task<OrderRow?> FindAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The customer's Draft order, or null when the customer has none.
    /// </summary>
    Task<OrderRow?> FindDraftAsync(string customerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// One line's row, by line id, or null when no order holds it.
    /// </summary>
    Task<OrderLineRow?> FindLineAsync(Guid lineId, CancellationToken cancellationToken = default);

    void Add(OrderRow row);
    void Remove(OrderRow row);

    /// <summary>
    /// Commits pending adds and removes. Assigns ids to added order rows.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory store standing in for a database, seeded with sample orders.
/// </summary>
/// <remarks>
/// A demonstration stand-in, not a model of a database. Rows returned by
/// <see cref="FindAsync"/> are the stored instances, so a mapped change lands in the
/// store immediately; only adds and removes wait for <see cref="SaveChangesAsync"/>.
/// There is no transaction, no rollback and no isolation between requests: enough
/// for one user of Design.Server, nothing more. A real application injects its
/// DbContext, scoped per request, behind the same interface.
/// Registered as a singleton on the server so it outlives each request's scope.
/// </remarks>
public class InMemoryOrderDbContext : IOrderDbContext
{
    /// <summary>Submitted orders, each with Widget A (10.00 × 2) and Widget B (25.00 × 1).</summary>
    public static readonly IReadOnlyList<int> SubmittedOrderIds = [100, 123, 200, 400, 456, 789];

    /// <summary>A Draft order with the same two lines; lines can be added and removed.</summary>
    public const int DraftOrderId = 300;

    private readonly Dictionary<int, OrderRow> _rows = [];
    private readonly List<OrderRow> _added = [];
    private readonly List<OrderRow> _removed = [];
    private readonly object _lock = new();
    private int _nextId = 1000;

    public InMemoryOrderDbContext()
    {
        foreach (var id in SubmittedOrderIds)
        {
            _rows[id] = SeedRow(id, OrderStatus.Submitted);
        }
        _rows[DraftOrderId] = SeedRow(DraftOrderId, OrderStatus.Draft);
    }

    public Task<OrderRow?> FindAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_rows.TryGetValue(id, out var row) ? row : null);
        }
    }

    public Task<OrderRow?> FindDraftAsync(string customerName, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_rows.Values.FirstOrDefault(
                r => r.CustomerName == customerName && r.Status == OrderStatus.Draft));
        }
    }

    public Task<OrderLineRow?> FindLineAsync(Guid lineId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_rows.Values.SelectMany(r => r.Lines).FirstOrDefault(l => l.Id == lineId));
        }
    }

    public void Add(OrderRow row)
    {
        lock (_lock) { _added.Add(row); }
    }

    public void Remove(OrderRow row)
    {
        lock (_lock) { _removed.Add(row); }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            foreach (var row in _added)
            {
                row.Id = _nextId++;
                _rows[row.Id] = row;
            }
            foreach (var row in _removed)
            {
                _rows.Remove(row.Id);
            }
            _added.Clear();
            _removed.Clear();
        }
        return Task.CompletedTask;
    }

    private static OrderRow SeedRow(int id, OrderStatus status)
    {
        var row = new OrderRow { Id = id, CustomerName = $"Customer_{id}", Status = status };
        row.Lines.Add(new OrderLineRow { Id = Guid.NewGuid(), ProductName = "Widget A", UnitPrice = 10.00m, Quantity = 2 });
        row.Lines.Add(new OrderLineRow { Id = Guid.NewGuid(), ProductName = "Widget B", UnitPrice = 25.00m, Quantity = 1 });
        return row;
    }
}
