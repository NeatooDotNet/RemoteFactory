using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Neatoo.RemoteFactory;
using OrderEntry.Ef;
using Microsoft.EntityFrameworkCore;

namespace OrderEntry.Domain;

/// <summary>
/// Order aggregate root.
/// Server-side methods use [Service] parameters for EF and child factories.
/// </summary>
[Factory]
internal class Order : IOrder
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Customer name is required")]
    public string? CustomerName { get; set { field = value; OnPropertyChanged(); } }

    public DateTime OrderDate { get; set { field = value; OnPropertyChanged(); } }

    public decimal Total => Lines?.Sum(l => l.LineTotal) ?? 0;

    /// <summary>
    /// Child collection of order lines.
    /// </summary>
    public IOrderLineList Lines { get; set; } = null!;

    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; } = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Server-side Create with child factory injection.
    /// Creates a new order with an empty Lines collection.
    /// </summary>
    [Remote, Create]
    internal void Create([Service] IOrderLineListFactory lineListFactory)
    {
        Id = Guid.NewGuid();
        OrderDate = DateTime.Now;
        Lines = lineListFactory.Create();
        IsNew = true;
    }

    /// <summary>
    /// Server-side Fetch with EF and child factory.
    /// Loads order and its lines from database.
    /// </summary>
    [Remote, Fetch]
    internal async Task Fetch(
        Guid id,
        [Service] IOrderEntryContext db,
        [Service] IOrderLineListFactory lineListFactory)
    {
        var entity = await db.Orders
            .Include(o => o.Lines)
            .FirstAsync(o => o.Id == id);

        Id = entity.Id;
        CustomerName = entity.CustomerName;
        OrderDate = entity.OrderDate;
        Lines = lineListFactory.Fetch(entity.Lines);
        IsNew = false;
    }

    /// <summary>
    /// Server-side Insert - persists new order and lines to database.
    /// </summary>
    [Remote, Insert]
    internal async Task Insert([Service] IOrderEntryContext db)
    {
        var entity = new OrderEntity
        {
            Id = Id,
            CustomerName = CustomerName!,
            OrderDate = OrderDate,
            Total = Total
        };

        // Add lines to entity
        foreach (var line in Lines)
        {
            entity.Lines.Add(new OrderLineEntity
            {
                Id = line.Id,
                OrderId = Id,
                ProductName = line.ProductName!,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice
            });
        }

        db.Orders.Add(entity);
        await db.SaveChangesAsync();
        IsNew = false;
    }

    /// <summary>
    /// Server-side Update - updates order and syncs lines.
    /// </summary>
    [Remote, Update]
    internal async Task Update([Service] IOrderEntryContext db)
    {
        var entity = await db.Orders
            .Include(o => o.Lines)
            .FirstAsync(o => o.Id == Id);

        entity.CustomerName = CustomerName!;
        entity.Total = Total;

        // Sync lines: remove deleted, update existing, add new
        var lineIds = Lines.Select(l => l.Id).ToHashSet();

        // Remove lines not in current collection
        var toRemove = entity.Lines.Where(e => !lineIds.Contains(e.Id)).ToList();
        foreach (var line in toRemove)
        {
            entity.Lines.Remove(line);
        }

        // Update existing and add new
        foreach (var line in Lines)
        {
            var existingLine = entity.Lines.FirstOrDefault(e => e.Id == line.Id);
            if (existingLine != null)
            {
                existingLine.ProductName = line.ProductName!;
                existingLine.Quantity = line.Quantity;
                existingLine.UnitPrice = line.UnitPrice;
            }
            else
            {
                entity.Lines.Add(new OrderLineEntity
                {
                    Id = line.Id,
                    OrderId = Id,
                    ProductName = line.ProductName!,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice
                });
            }
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Server-side Delete - removes order from database.
    /// Lines are cascade deleted by EF.
    /// </summary>
    [Remote, Delete]
    internal async Task Delete([Service] IOrderEntryContext db)
    {
        var entity = await db.Orders.FirstAsync(o => o.Id == Id);
        db.Orders.Remove(entity);
        await db.SaveChangesAsync();
    }
}
