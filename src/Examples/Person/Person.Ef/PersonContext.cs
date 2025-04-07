
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;

namespace Person.Ef;

public interface IPersonContext
{
	DbSet<PersonEntity> Persons { get; set; }

	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class PersonContext : DbContext, IPersonContext
{
	public virtual DbSet<PersonEntity> Persons { get; set; } = null!;

	public string DbPath { get; }

	public PersonContext()
	{
		var folder = Environment.SpecialFolder.LocalApplicationData;
		var path = Environment.GetFolderPath(folder);
		this.DbPath = System.IO.Path.Join(path, "Person.db");
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

public class PersonEntity : IdPropertyChangedBase
{
	[Required]
	public string FirstName { get; set; } = null!;
	[Required]
	public string LastName { get; set; } = null!;
	public string Email { get; set; } = null!;
	public string Phone { get; set; } = null!;
	public string? Notes { get; set; }
	public DateTime Created { get; set; }
	public DateTime Modified { get; set; }
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