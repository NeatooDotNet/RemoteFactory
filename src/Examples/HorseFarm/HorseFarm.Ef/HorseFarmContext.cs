using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HorseFarm.Ef;

public class HorseFarmContext : DbContext, IHorseFarmContext
{
	public DbSet<HorseFarmEntity> HorseFarms { get; set; }
	public DbSet<HorseEntity> Horses { get; set; }
	public DbSet<CartEntity> Carts { get; set; }
	public DbSet<PastureEntity> Pastures { get; set; }

	public string DbPath { get; }

	public HorseFarmContext()
	{
		var folder = Environment.SpecialFolder.LocalApplicationData;
		var path = Environment.GetFolderPath(folder);
	  this.DbPath = System.IO.Path.Join(path, "HorseFarm.db");
	}

	// The following configures EF to create a Sqlite database file in the
	// special "local" folder for your platform.
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		 => optionsBuilder.UseSqlite($"Data Source={this.DbPath}")
		 .UseLazyLoadingProxies();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Ef doesn't use property getter/setters by default
		// https://stackoverflow.com/questions/47382680/entity-framework-core-property-setter-is-never-called-violation-of-encapsulat
		foreach (var entityType in modelBuilder.Model.GetEntityTypes())
		{
			if (entityType.ClrType.IsAssignableTo(typeof(IdPropertyChangedBase)))
			{
				var property = entityType.FindProperty(nameof(IdPropertyChangedBase.Id));

				property?.SetPropertyAccessMode(PropertyAccessMode.PreferFieldDuringConstruction);

			}
		}
	}
}

[Table("HorseFarm")]
public class HorseFarmEntity : IdPropertyChangedBase
{
	public virtual PastureEntity Pasture { get; set; } = null!;
	public virtual List<CartEntity> Carts { get; } = [];
}

[Table("Horse")]
public class HorseEntity : IdPropertyChangedBase
{
	[Required]
	[MaxLength(100)]
	public string Name { get; set; } = null!;

	[Required]
	public DateOnly BirthDate { get; set; }

	[Required]
	public int Breed { get; set; }

	public int? CartId { get; set; }
	public virtual CartEntity? Cart { get; set; }
	public virtual PastureEntity? Pasture { get; set; }

}

[Table("Cart")]
public class CartEntity : IdPropertyChangedBase
{

	[Required]
	[MaxLength(100)]
	public string Name { get; set; } = null!;

	[Required]
	public int CartType { get; set; }

	[Required]
	public int NumberOfHorses { get; set; }

	public virtual ICollection<HorseEntity> Horses { get; } = [];

	public int HorseFarmId { get; set; }

	public virtual HorseFarmEntity HorseFarm { get; set; } = null!;

}

[Table("Pasture")]
public class PastureEntity : IdPropertyChangedBase
{
	public int HorseFarmId { get; set; }
	public virtual HorseFarmEntity HorseFarm { get; set; } = null!;
	public virtual ICollection<HorseEntity> Horses { get; } = [];
}

public abstract class IdPropertyChangedBase : INotifyPropertyChanged
{
	private int _id;

	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id
	{
		get => this._id;
		set
		{
			this._id = value;
			this.OnPropertyChanged();
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;
	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
