using Microsoft.EntityFrameworkCore;

namespace HorseFarm.Ef;

public interface IHorseFarmContext
{
    DbSet<CartEntity> Carts { get; set; }
    DbSet<HorseFarmEntity> HorseFarms { get; set; }
    DbSet<HorseEntity> Horses { get; set; }
    DbSet<PastureEntity> Pastures { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}