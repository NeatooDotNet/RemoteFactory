using Microsoft.EntityFrameworkCore;

namespace OrderEntry.Ef;

/// <summary>
/// Order entry database context interface.
/// Used for dependency injection in domain layer.
/// </summary>
public interface IOrderEntryContext
{
    DbSet<OrderEntity> Orders { get; }
    DbSet<OrderLineEntity> OrderLines { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Order entry database context implementation.
/// </summary>
public class OrderEntryContext : DbContext, IOrderEntryContext
{
    public DbSet<OrderEntity> Orders { get; set; } = null!;
    public DbSet<OrderLineEntity> OrderLines { get; set; } = null!;

    public string DbPath { get; }

    public OrderEntryContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "OrderEntry.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source={DbPath}")
        .UseLazyLoadingProxies();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderEntity>()
            .Property(o => o.Total)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderEntity>()
            .HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderLineEntity>()
            .Property(l => l.UnitPrice)
            .HasPrecision(18, 2);
    }
}
